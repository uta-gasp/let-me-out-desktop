# Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser

if ($PSVersionTable.PSVersion.Major -lt 4) {
    "PwerShell version 4+ is required"
    Exit
}

$name = "let-me-out"
$file = ".\" + $name + ".zip"
$dir = (Get-Item -Path ".\").FullName + "\" + $name
$path = "let-me-out.zip http://www.sis.uta.fi/~csolsp/shared/projects/gasp/letmeout/" + $name + ".zip"

Invoke-WebRequest -OutFile $path

Get-ChildItem $dir -ErrorAction SilentlyContinue | Remove-Item -Recurse

if ($PSVersionTable.PSVersion.Major -eq 4) {

    "We are on PwerShell version 4. OK."
    
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    function Unzip
    {
        param([string]$zipfile, [string]$outpath)

        [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
    }

    Unzip $file $dir
}
else {

    "We are on PwerShell version 5. Nice."
    
    Expand-Archive $file -DestinationPath $dir
}

Remove-Item $file

Pause
