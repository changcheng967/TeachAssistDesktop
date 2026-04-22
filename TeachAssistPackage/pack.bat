@echo off
set MAKEAPPX=C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe
"%MAKEAPPX%" pack /d "..\TeachAssistApp\bin\Release\net10.0-windows10.0.17763\win-x64\publish" /p "TeachAssistDesktop_5.4.0.0_x64.msix" /o
