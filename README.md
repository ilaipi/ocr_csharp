# ocr_csharp
ocr by c#

## run
1, install packages    
Open the Package Manager Console - From Tools > NuGet Package Manager, select Package Manager Console and run:    
`update-package -reinstall`

2, download tessdata    
[download page](https://tesseract-ocr.github.io/tessdoc/Data-Files#data-files-for-version-304305)

```
mkdir ./Properties/tessdata/
mv eng.traineddata to ./Properties/tessdata/
mv chi_sim.traineddata to ./Properties/tessdata/
```

3, open Progrma.cs    
press CTRL+F5
