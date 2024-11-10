$SCRIPT_VERSION = "1.0.0"
$API_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models"
$MODEL = "gemini-1.5-flash-latest"

$MAX_RETRIES = 3
$TIMEOUT_SECONDS = 30
$MAX_DIFF_LENGTH = 30000

$LOG_FILE = "commit_message.log"

$logBuffer = New-Object System.Text.StringBuilder

function Write-Log {
    param (
        [string]$message,
        [switch]$Console
    )
    $timestamp = Get-Date -Format "HH:mm:ss"
    $logMessage = "${timestamp}: ${message}"
    $logBuffer.AppendLine($logMessage) | Out-Null
    if ($Console) {
        Write-Host $message
    }
}

function Invoke-ApiRequestWithRetries {
    param (
        [System.Net.Http.HttpContent]$content,
        [int]$maxRetries = $MAX_RETRIES,
        [int]$timeoutSeconds = $TIMEOUT_SECONDS
    )

    $apiKey = [System.Environment]::GetEnvironmentVariable("COMMIT_API_KEY")
    $apiUrl = "${API_BASE_URL}/${MODEL}:generateContent?key=$apiKey"

    $httpClient = New-Object System.Net.Http.HttpClient
    $httpClient.Timeout = [TimeSpan]::FromSeconds($timeoutSeconds)

    for ($attempt = 1; $attempt -le $maxRetries; $attempt++) {
        Write-Log "Попытка $attempt из $maxRetries"
        Write-Log "Отправка запроса..." -Console
        try {
            $task = $httpClient.PostAsync($apiUrl, $content)
            $response = $task.GetAwaiter().GetResult()

            if ($response.IsSuccessStatusCode) {
                Write-Log "Успешный ответ: $($response.StatusCode)"
                return $response
            }
            Write-Log "Ошибка запроса: $($response.StatusCode)"
            $errorContent = $response.Content.ReadAsStringAsync().Result
            Write-Log "Содержимое ошибки: $errorContent"
        }
        catch {
            Write-Log "Произошла ошибка во время запроса: $_"
            if ($_.Exception.InnerException) {
                Write-Log "Внутреннее исключение: $($_.Exception.InnerException)"
            }
            Write-Log "Стек вызовов: $($_.ScriptStackTrace)"
        }
        if ($attempt -lt $maxRetries) {
            Write-Log "Ждём перед следующей попыткой..."
            Start-Sleep -Seconds 1
        }
    }
    throw "Все $maxRetries попытки запроса к API завершились неудачно"
}

function Get-GitDiff {
    Write-Log "Получаем git diff HEAD"
    Write-Log "Получение изменений..." -Console
    $diff = git diff HEAD
    if ($diff.Length -eq 0) {
        throw "git diff HEAD не вернул изменений"
    }
    if ($diff.Length -gt $MAX_DIFF_LENGTH) {
        Write-Log "Ограничиваем diff до $MAX_DIFF_LENGTH символов"
        $diff = $diff.Substring(0, $MAX_DIFF_LENGTH)
    }
    return $diff
}

function Get-UserPrompt {
    Write-Host "Введите дополнительную информацию для коммита (необязательно, нажмите Enter для пропуска):"
    $userPrompt = Read-Host
    return $userPrompt
}

function Get-CommitMessage {
    $apiKey = [System.Environment]::GetEnvironmentVariable("COMMIT_API_KEY")
    if (-not $apiKey) {
        throw "Переменная среды COMMIT_API_KEY не задана. Пример: AIzaSyD..."
    }

    $diff = Get-GitDiff
    $userPrompt = Get-UserPrompt

    $systemPrompt = @"
Create a concise and informative commit message following the Conventional Commits specification, but in Russian.

Input: You will receive the output of 'git diff HEAD' and optional user input.
Task: Analyze the diff and create a commit message that neutrally describes the changes.

Requirements:
- Use the format: <type>[optional scope]: <description>
- Each line should be no longer than 75 characters.
- Provide a brief description in the first line.
- Use passive voice or neutral statements (e.g., "file added", "function updated").
- Be specific but concise.
- Do not use codeblocks (```) or other formatting.
- If user input is provided, it should be the main focus of the commit message.
- Emphasize the user's reason for changes and describe how the code changes support this reason.

Example format:
фича(авторизация): реализована функциональность сброса пароля
- Добавлен API-эндпоинт для сброса пароля в соответствии с требованиями безопасности
- Создан шаблон email с инструкциями по сбросу для улучшения UX
- Обновлена модель пользователя с полем токена сброса для надежного хранения

User input (if provided): $userPrompt

Important: If user input is provided, make it the central theme of the commit message,
explaining how the code changes relate to and support the user's stated reason or goal.
"@

    $requestBody = @{
        contents = @(
            @{
                parts = @(
                    @{
                        text = "$systemPrompt`n`n$diff"
                    }
                )
            }
        )
    }

    $jsonBody = $requestBody | ConvertTo-Json -Depth 10
    $content = New-Object System.Net.Http.StringContent($jsonBody, [System.Text.Encoding]::UTF8, "application/json")

    Write-Log "Отправляем запрос к API"
    $response = Invoke-ApiRequestWithRetries -content $content

    Write-Log "Читаем потоковый ответ"
    $responseStream = $response.Content.ReadAsStreamAsync().Result
    $reader = [System.IO.StreamReader]::new($responseStream)
    $responseBody = $reader.ReadToEnd()

    Write-Log "Полный ответ:"
    Write-Log $responseBody

    $responseJson = $responseBody | ConvertFrom-Json
    if (-not $responseJson.candidates) {
        throw "Не удалось получить сообщение для коммита"
    }

    $commitMessage = $responseJson.candidates[0].content.parts[0].text
    return $commitMessage
}

function Main {
    try {
        Add-Type -AssemblyName System.Net.Http

        $diagnosticInfo = Get-DiagnosticInfo
        Write-Log $diagnosticInfo -Console
        Write-Log $diagnosticInfo

        $commitMessage = Get-CommitMessage
        Set-Clipboard -Value $commitMessage
        Write-Log "Сообщение для коммита скопировано в буфер обмена"
        Write-Log "Сообщение для коммита:" -Console
        Write-Log $commitMessage -Console
    }
    catch {
        Write-Log "Произошла ошибка: $_"
        $logBuffer.ToString() | Out-File -FilePath $LOG_FILE
        Write-Log "Произошла ошибка. Подробности в файле: $LOG_FILE" -Console
    }
    finally {
        Write-Host "Завершение работы..."
    }
}

function Get-DiagnosticInfo {
    $psVersion = $PSVersionTable.PSVersion
    $osInfo = Get-CimInstance Win32_OperatingSystem

    try {
        $gitVersion = (git --version 2>$null) -replace 'git version '
        if ([string]::IsNullOrEmpty($gitVersion)) {
            $gitInfo = "Git не установлен или не найден в PATH"
        }
        else {
            $gitInfo = "Версия Git: $gitVersion"
        }
    }
    catch {
        $gitInfo = "Не удалось получить информацию о Git: $_"
    }

    $info = @"
Диагностическая информация:
- Версия скрипта: $SCRIPT_VERSION
- Версия PowerShell: $psVersion
- ОС: $($osInfo.Caption) $($osInfo.Version)
- $gitInfo
- Модель API: $MODEL
"@
    return $info
}

Main
