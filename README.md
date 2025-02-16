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

# Unfinished Tasks

The optional frontend tasks are left unfinished. Task a involves building a simple form and submitting an HTTP POST request to the REST API that I have already built and is quite simple so I do not feel like I can demonstrate much by doing this task. A framework would be overkill for this kind of task but I could use Vue.js or ReactJS.

Task d is also quite easy. I would use [chart.js](https://www.chartjs.org/) for this. In my personal projects, I have [similar applications of on-demand charting](https://github.com/JoseiToAoiTori/AyaseChihaya/blob/master/general/activity.js) than what this assignment asks for. However, I would need to store currency data over multiple days to be able to make a proper chart out of this and I've decided to simply offer an explanation instead of working on this project for too long.

# Unit Testing

Even though it wasn't a part of the assignment, I considered adding unit tests using NUnit or XUnit, complete with mocks. `dotnet new nunit --name webapi.Tests` would scaffold a new test project which can then reference the main project. However, I have made quite a few mistakes when writing my code that reduce its testability. I would first need to refactor the webapi project into its own folder, delete the solution, create a new solution to reference the webapi project, create a new NUnit project and have it reference the main project and then finally I would need to 
