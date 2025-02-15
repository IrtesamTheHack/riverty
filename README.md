# Requirements

.NET SDK 8

# Instructions

Go into the `consoleapp` directory and do the following:

```
dotnet restore
dotnet run
```

For `webapi`, go into the directory and do the following:

```
dotnet restore
dotnet ef database update
dotnet run
```

Make sure you have `dotnet-ef` installed. If not, do:

```
dotnet tool install --global dotnet-ef
```
