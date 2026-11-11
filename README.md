# Desktop Web Browser in C#

Coursework for Industrial Programming (F21SC, Heriot-Watt University), autumn 2025. A small desktop web browser written in C# with Windows Forms. It fetches pages over HTTP, shows the raw HTML, and keeps each user's bookmarks and history in a SQLite database.

## What it does

- Sends HTTP requests with a 30 second timeout. It follows redirects, keeps cookies and handles compressed responses.
- Turns failures into a readable error page. A bad URL shows as 400, a timeout as 408 and a network or DNS failure as 503. Server errors keep their own code. Each error page lists the status code, the reason and some troubleshooting steps.
- Shows the page title, the raw HTML and the first five links on the page. Double-click a link to open it.
- Back and forward navigation, a home page you can change, bookmarks you can add, edit and delete, and a history you can search.
- Several user profiles. Each one has its own bookmarks, history and settings.

## How it is built

- `src/Services/HttpService.cs`: requests and error handling
- `src/Services/DatabaseService.cs`: SQLite through Entity Framework Core, with tables for users, bookmarks, history and settings. Every history row stores the status code and load time.
- `src/DataStructures/`: a linked list for history, two stacks for back and forward, and a tree for bookmarks. These are written by hand instead of using the built-in collections.
- `src/Views/`: the Windows Forms interface
- `tests/DataStructureTests.cs`: 12 MSTest unit tests for the data structures

## Running

Needs Windows and the .NET 8 SDK.

    dotnet run

To run the tests:

    dotnet test

## Notes

This repo holds the source code and tests only. The coursework report and brief are not included.
