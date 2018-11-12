@echo off
cd Build
call "C:\Program Files\7-Zip\7z.exe" a let-me-out-desktop.zip *
scp2 -d -m644 let-me-out-desktop.zip csolsp@shell.sis.uta.fi:/home/staff/csolsp/public_html/shared/projects/gasp/letmeout
del let-me-out-desktop.zip
cd ..
pause
