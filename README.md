# Movie Night Agent - C# Manual Tool Calling Demo

This project demonstrates a manual AI agent loop in C#.

The agent helps choose a movie or episode for movie night by using three mocked tools:

1. `search_library` - searches a mocked personal media library.
2. `get_title_details` - gets details for a selected title.
3. `add_to_watchlist` - simulates adding a title to a watchlist.

Task 1: Manual loop using direct HTTP and Anthropic Messages API.
Task 2: Bonus version using Microsoft Agent Framework with OpenAI provider.

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

## Architecture

This repository contains two implementations of the same “Movie Night Agent” idea.

The goal is to demonstrate how an AI agent can reason over a user request, use tools, and return a final answer based on real tool results.

---

## Task 1 Architecture — Manual Agent Loop

Task 1 implements the agent loop manually in C#.

### Main flow

```text
User request
    ↓
Manual C# agent loop
    ↓
LLM response
    ↓
Tool request detected
    ↓
C# executes matching tool
    ↓
Tool result is sent back to the model
    ↓
Repeat until final response
    ↓
Strongly typed final response
```

### Main components

```text
Program.cs
    - Sends requests to the model
    - Reads model responses
    - Detects tool calls
    - Executes tools manually
    - Sends tool results back to the model
    - Parses the final JSON answer

Tools/MovieNightTools.cs
    - search_library
    - get_title_details
    - add_to_watchlist

Models/MovieNightFinalResponse.cs
    - Strongly typed final response model
```

### Tools

The manual loop exposes three mocked tools:

```text
search_library
    Searches the mocked media library by genre, mood, runtime, and type.

get_title_details
    Gets full information for a selected media title.

add_to_watchlist
    Simulates adding the selected title to a watchlist.
```

### Why this approach matters

In Task 1, the tool loop is implemented directly in C# instead of relying on an agent framework. This shows the internal mechanics of an agent:

```text
model decides → C# executes → result returns to model → model continues
```

The console logs show each model/tool cycle clearly.

---

## Task 2 Architecture — Microsoft Agent Framework + Local Ollama Model

Task 2 implements the agent using Microsoft Agent Framework.

Instead of manually managing every model/tool cycle, the project uses `ChatClientAgent` from `Microsoft.Agents.AI`.

### Main flow

```text
User request
    ↓
ChatClientAgent
    ↓
OllamaSharp IChatClient
    ↓
Local Ollama model: qwen2.5:7b
    ↓
Agent calls registered C# tools
    ↓
Tool results return to the agent
    ↓
Final agent response
```

### Main components

```text
Task2-MAF/Program.cs
    - Creates the local Ollama chat client
    - Wraps it with Microsoft.Extensions.AI function invocation
    - Creates the Microsoft Agent Framework ChatClientAgent
    - Registers C# methods as AITool tools
    - Runs the agent

Task2-MAF/Tools/MovieNightTools.cs
    - Contains the same mocked movie library logic

Task2-MAF/Models/MovieNightFinalResponse.cs
    - Defines the final response shape
```

### Framework usage

Task 2 uses Microsoft Agent Framework here:

```csharp
using Microsoft.Agents.AI;
```

The main framework class is:

```csharp
ChatClientAgent agent = new(...);
```

The C# methods are exposed to the agent as tools using:

```csharp
AIFunctionFactory.Create(...)
```

The agent is executed with:

```csharp
AgentResponse response = await agent.RunAsync(...);
```

### Task 2 architecture diagram

```text
C# methods
    ↓
AIFunctionFactory.Create(...)
    ↓
AITool[]
    ↓
ChatClientAgent
    ↓
OllamaSharp IChatClient
    ↓
qwen2.5:7b local model
```

### Tools registered in MAF

```text
search_funny_episodes
    AI-facing wrapper tool.
    It searches for funny comedy episodes under a maximum runtime.

get_title_details
    Gets full title details by id.

add_to_watchlist
    Adds the selected title to the Tonight watchlist.
```

### Why qwen2.5:7b was used

The first local model tested was `llama3.2:3b`. It could call one tool, but it was not reliable enough for multi-step tool calling and sometimes invented titles.

The project was switched to `qwen2.5:7b`, which handled the full tool sequence correctly:

```text
search_funny_episodes
    ↓
get_title_details
    ↓
add_to_watchlist
    ↓
final answer
```

### Local model setup

Task 2 uses Ollama locally:

```bash
ollama pull qwen2.5:7b
```

The C# project connects to Ollama through:

```text
http://localhost:11434
```

No cloud API key is required for Task 2.

---

## Comparison

| Part             | Task 1                          | Task 2                              |
| ---------------- | ------------------------------- | ----------------------------------- |
| Agent style      | Manual loop                     | Microsoft Agent Framework           |
| Model provider   | External LLM API                | Local Ollama model                  |
| Tool handling    | Manually parsed and executed    | Registered as `AITool`s             |
| Main agent class | Custom C# loop                  | `ChatClientAgent`                   |
| Final response   | Strongly typed JSON model       | Agent text response                 |
| Purpose          | Show how agents work internally | Show framework-based implementation |

---

## Demo Notes

For Task 1, show the manual loop logs:

```text
Model cycle
Tool requested
Tool executed
Tool result returned
Final structured response
```

For Task 2, show the Microsoft Agent Framework tool calls:

```text
[MAF TOOL EXECUTED] search_funny_episodes(30)
[MAF TOOL EXECUTED] get_title_details(office-stress-relief)
[MAF TOOL EXECUTED] add_to_watchlist(office-stress-relief, Tonight)
```

This proves that the MAF agent successfully used the registered C# tools with a local model.
