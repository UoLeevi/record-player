# RecordPlayer

## Deployment

1. Publish project in Visual Studio
2. Copy files to Raspberry Pi

```
scp -r .\RecordPlayer\bin\Release\net8.0\publish\linux-arm64\* pi@raspberrypi:~/RecordPlayer
```