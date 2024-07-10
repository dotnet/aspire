@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0mark-shipped.ps1"