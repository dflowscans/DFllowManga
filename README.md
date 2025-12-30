# DFlowMangaCMS
A custom MangaCMS built on NET.ASP MVC and Razor pages.

#### Note: Only this README will be updated. The others you see in this repo will NOT be updated.

## Setting ENV variables:

Update `Program.cs`'s variables with you database's FTP/SSH credentials.<br>
Also make sure to add the IP and PORT your database is hosted on.

## Compiling the code:
#### Note: This is not neccessary but it is recommended as sometimes I'll probably forget to compile it myself and you'll be using an older version.
#### Note: All commands should be run in the root directory of the project.

To get started run:
```
dotnet publish -c Release -r linux-x64 --self-contained true -o .\publish
```

This will generate the following folders:
- bin
- publish
- wwwroot

## Hosting:

#### Note: This project has only been tested on dedicated VPS running Ubuntu!

Using any software that allows file transfers using FTP<br>
1. Move the contents of `MangaReader/bin/Release/net10.0/linux-x64/` folder into the root folder of your projec on your VPS.<br>
2. Move both folders `wwwroot` and `publish` in the same folder.
