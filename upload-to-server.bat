@echo off
cd Build
call 7z a let-me-out.zip let-me-out.exe UnityPlayer.dll let-me-out_Data
scp2 -d -m644 let-me-out.zip csolsp@shell.sis.uta.fi:/home/staff/csolsp/public_html/shared/projects/gasp/letmeout
del let-me-out.zip
pause
