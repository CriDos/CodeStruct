# CodeStruct

CodeStruct is a versatile utility that scans a portion of your source code files, generates a file structure along with its content, and copies it to the clipboard. The compiled data provides a convenient representation of the project's structure and code, making it easy to share and collaborate in various platforms, such as ChatGPT or other text-based communication tools, without losing the context of the code.

## Features

- Customizable file extensions and ignored directories
- Scans and generates source code structure and content
- Copies the output to the clipboard for easy input to language models

## Requirements

- .NET 7.0 Runtime (to run the prebuilt version)
- .NET 7.0 SDK (to build from source)

## Installation

1. Clone the repository or download the source code:

```
git clone https://github.com/CriDos/CodeStruct.git
```

2. Navigate to the project folder and build the project using the provided `build.cmd` script:

```
cd CodeStruct
build.cmd
```

3. After the build is complete (using .NET 7.0 SDK), you will find the executable file `CodeStruct.exe` in the `win-x64` folder.

## Prebuilt Version

You can download the precompiled version of CodeStruct for Windows 64-bit from the [Releases](https://github.com/CriDos/CodeStruct/releases) page. This version requires the .NET 7.0 Runtime to be installed on your system.

## Usage

1. Open a command prompt or terminal and navigate to the folder containing your source code.

2. Run the CodeStruct executable from the source code folder:

```
path\to\CodeStruct.exe
```

3. CodeStruct will scan the source code files and generate a file structure based on the allowed file extensions and ignoring the specified directories.

4. The generated data will be copied to the clipboard, which can then be fed into your desired language model.

5. Customize the allowed file extensions and ignored directories by editing the `AllowedExtensions.txt` and `IgnoredDirectories.txt` files in the CodeStruct application folder.

## Example

Suppose you have the following source code files:

```
myproject/
│
├─src/
│  ├─main.py
│  └─utils.py
│
└─tests/
   └─test_main.py
```

After running CodeStruct in the `myproject` folder, it will generate and copy the following structure to the clipboard:

```text
src/main.py
\```
# Contents of main.py
\```

src/utils.py
\```
# Contents of utils.py
\```

tests/test_main.py
\```
# Contents of test_main.py
\```
```

You can now effortlessly paste this formatted text into various platforms, such as text-based communication tools or collaboration services, for easier sharing, editing, and collaboration on your project.
