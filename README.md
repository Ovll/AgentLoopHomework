# Movie Night Agent - C# Manual Tool Calling Demo

This project demonstrates a manual AI agent loop in C#.

The agent helps choose a movie or episode for movie night by using three mocked tools:

1. `search_library` - searches a mocked personal media library.
2. `get_title_details` - gets details for a selected title.
3. `add_to_watchlist` - simulates adding a title to a watchlist.

## Features

- C# .NET console application
- Manual model/tool loop using HTTP requests
- Three mocked C# tools
- Logs every model/tool cycle
- Sends tool results back to the model manually
- Final answer is valid JSON
- Final JSON is parsed into a strongly typed C# model

## Setup

Create a local `.env` file in the project root:

```env
ANTHROPIC_API_KEY=your-api-key-here
```