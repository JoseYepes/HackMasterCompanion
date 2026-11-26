# Genera los iconos de la aplicación instalable (F013, specs/011-instalable-sin-conexion/research.md R5):
# un dado de 20 caras dorado sobre granate, con los colores del estilo (F007). Usa System.Drawing de
# Windows PowerShell, sin descargas. El dibujo cabe en el 80 % central, así sirve también de icono
# «maskable» (Android lo recorta en círculo).
#
#   powershell -File tools/Icons/New-AppIcons.ps1

Add-Type -AssemblyName System.Drawing

$wwwroot = Join-Path $PSScriptRoot '..\..\src\HackMaster.Web\wwwroot'
$granate = [System.Drawing.ColorTranslator]::FromHtml('#5a1f1b')
$gold = [System.Drawing.ColorTranslator]::FromHtml('#c9a24a')
$goldLight = [System.Drawing.ColorTranslator]::FromHtml('#f0c75e')

function Get-Point([double]$angleDegrees, [double]$radius, [double]$center) {
    $radians = $angleDegrees * [Math]::PI / 180
    return New-Object System.Drawing.PointF ([single]($center + $radius * [Math]::Cos($radians))), ([single]($center + $radius * [Math]::Sin($radians)))
}

function New-Icon([int]$size, [string]$name) {
    $bitmap = New-Object System.Drawing.Bitmap $size, $size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear($granate)

    $center = $size / 2
    $outer = $size * 0.38
    $inner = $outer * 0.56
    $hexagon = @(0..5 | ForEach-Object { Get-Point (-90 + 60 * $_) $outer $center })
    $triangle = @(0..2 | ForEach-Object { Get-Point (-90 + 120 * $_) $inner $center })

    $graphics.FillPolygon((New-Object System.Drawing.SolidBrush $gold), $hexagon)
    $graphics.FillPolygon((New-Object System.Drawing.SolidBrush $goldLight), $triangle)

    $pen = New-Object System.Drawing.Pen $granate, ([single]($size * 0.018))
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawPolygon($pen, $hexagon)
    $graphics.DrawPolygon($pen, $triangle)
    # Cada vértice del triángulo central se une a su vértice del hexágono y a los dos vecinos.
    for ($i = 0; $i -lt 3; $i++) {
        $k = 2 * $i
        foreach ($j in @((($k + 5) % 6), $k, (($k + 1) % 6))) {
            $graphics.DrawLine($pen, $triangle[$i], $hexagon[$j])
        }
    }

    $font = New-Object System.Drawing.Font 'Georgia', ([single]($size * 0.13)), ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $textBox = New-Object System.Drawing.RectangleF ([single]($center - $inner)), ([single]($center - $inner * 0.35)), ([single](2 * $inner)), ([single]($inner * 1.1))
    $graphics.DrawString('20', $font, (New-Object System.Drawing.SolidBrush $granate), $textBox, $format)

    $path = Join-Path $wwwroot $name
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    Write-Output "Escrito $path"
}

New-Icon 192 'icon-192.png'
New-Icon 512 'icon-512.png'
