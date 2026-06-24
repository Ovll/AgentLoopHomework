# Movie Night Agent - C# AI Agent Demo

This repository demonstrates two versions of the same C# AI agent idea.

The agent helps choose a movie-night episode by using mocked C# tools.

## Branches

| Branch      | Description                                                                |
| ----------- | -------------------------------------------------------------------------- |
| `main`      | Task 1: manual C# agent loop.                                              |
| `task2-maf` | Task 2 bonus: Microsoft Agent Framework version with a local Ollama model. |

The same README is used to describe both branches.

## Project Idea

The agent receives this type of request:

```text
Find me a funny episode under 30 minutes for tonight.
Check that the best choice has English subtitles and HD quality.
Then add it to my Tonight watchlist.
```

The project uses three mocked tools:

| Tool                | Purpose                                  |
| ------------------- | ---------------------------------------- |
| `search_library`    | Searches a mocked media library.         |
| `get_title_details` | Gets full details for a selected title.  |
| `add_to_watchlist`  | Simulates adding a title to a watchlist. |

---

# Task 1 - Manual Agent Loop

Task 1 is implemented on the `main` branch.

It manually implements the model/tool loop in C#.

## Task 1 Architecture

```text
User request
    ↓
Program.cs manual loop
    ↓
Send request to model
    ↓
Model requests a tool
    ↓
C# executes the tool
    ↓
Tool result is sent back to the model
    ↓
Repeat until final answer
    ↓
Parse final JSON into strongly typed C# model
```

## Task 1 Main Files

```text
Program.cs
    Manual agent loop.
    Sends HTTP requests to the model.
    Detects tool calls.
    Executes the correct C# tool.
    Sends tool results back to the model.
    Parses the final JSON response.

Tools/MovieNightTools.cs
    Mocked C# tools:
    - search_library
    - get_title_details
    - add_to_watchlist

Models/MovieNightFinalResponse.cs
    Strongly typed model for the final response.
```

## Task 1 Features

* Manual model/tool loop.
* Direct HTTP API call.
* Three mocked C# tools.
* Console logging for every model/tool cycle.
* Final answer returned as JSON.
* Final JSON parsed into a strongly typed C# object.

## Task 1 Setup

Create a `.env` file in the project root:

```env
ANTHROPIC_API_KEY=your-api-key-here
```

Run:

```bash
dotnet run
```

---

# Task 2 - Microsoft Agent Framework Bonus

Task 2 is implemented on the `task2-maf` branch.

It uses Microsoft Agent Framework with a local Ollama model.

No cloud API key is required for Task 2.

## Task 2 Architecture

```text
User request
    ↓
ChatClientAgent (system prompt sets persona and workflow)
    ↓
Microsoft.Extensions.AI function invocation middleware
    ↓
OllamaSharp IChatClient → Local Ollama model: qwen2.5:7b
    ↓
Registered C# tools are called automatically
    ↓
Tool results return to the agent
    ↓
Agent summary (prose)
    ↓
GetResponseAsync<MovieNightFinalResponse>  ← structured output step
    ↓
Strongly typed C# object
```

## Where Microsoft Agent Framework Is Used

Task 2 uses Microsoft Agent Framework through:

```csharp
using Microsoft.Agents.AI;
```

The main framework class is:

```csharp
ChatClientAgent agent = new(...);
```

The tools are registered as `AITool` instances:

```csharp
AITool tool = AIFunctionFactory.Create(...);
```

The agent is executed in two steps:

```csharp
// Step 1: tool loop — MAF handles function invocation automatically
AgentResponse agentResponse = await agent.RunAsync(...);

// Step 2: structured output — reformat prose summary into a typed C# object
ChatResponse<MovieNightFinalResponse> typed = await baseChatClient
    .GetResponseAsync<MovieNightFinalResponse>(formatMessages, useJsonSchemaResponseFormat: false);
```

`useJsonSchemaResponseFormat: false` is used because `qwen2.5:7b` via Ollama cannot
combine tool calling and schema-constrained output in one pass. The two-step approach
keeps them separate: the agent loop runs unconstrained, then a second call handles typing.

## Task 2 Main Files

```text
Task2-MAF/Program.cs
    Creates the Ollama chat client.
    Wraps it with function invocation.
    Creates the ChatClientAgent.
    Registers C# methods as tools.
    Runs the agent.

Task2-MAF/Tools/MovieNightTools.cs
    Mocked movie library logic.

Task2-MAF/Models/MovieNightFinalResponse.cs
    Final response model.
```

## Task 2 Registered Tools

| Tool                    | Purpose                                                                        |
| ----------------------- | ------------------------------------------------------------------------------ |
| `search_funny_episodes` | AI-facing wrapper that searches funny comedy episodes under a maximum runtime. |
| `get_title_details`     | Gets full details for a selected title id.                                     |
| `add_to_watchlist`      | Adds the selected title to the Tonight watchlist.                              |

## Task 2 Local Model

Task 2 uses Ollama locally.

The model used:

```text
qwen2.5:7b
```

Install/pull it with:

```bash
ollama pull qwen2.5:7b
```

The C# project connects to Ollama at:

```text
http://localhost:11434
```

## Why qwen2.5:7b

A smaller model, `llama3.2:3b`, was tested first. It could call one tool, but it was not reliable enough for the full multi-step tool sequence.

`qwen2.5:7b` handled the full sequence correctly:

```text
search_funny_episodes
    ↓
get_title_details
    ↓
add_to_watchlist
    ↓
final answer
```

## Task 2 Run

From the Task 2 folder:

```bash
cd Task2-MAF
dotnet run
```

Expected console logs include:

```text
[MAF TOOL EXECUTED] search_funny_episodes(30)
[MAF TOOL EXECUTED] get_title_details(office-stress-relief)
[MAF TOOL EXECUTED] add_to_watchlist(office-stress-relief, Tonight)
```

---

# Comparison

| Part             | Task 1                                  | Task 2                                    |
| ---------------- | --------------------------------------- | ----------------------------------------- |
| Branch           | `main`                                  | `task2-maf`                               |
| Agent style      | Manual loop                             | Microsoft Agent Framework                 |
| Model connection | Direct HTTP API                         | OllamaSharp `IChatClient`                 |
| Model            | External LLM                            | Local Ollama model                        |
| Tool handling    | Manually parsed and executed            | Registered as `AITool`s                   |
| Main agent logic | Custom C# loop                          | `ChatClientAgent`                         |
| System prompt    | Inline in user message                  | Explicit `systemPrompt` variable          |
| Final response   | Strongly typed JSON object              | Strongly typed via `GetResponseAsync<T>`  |
| Purpose          | Show how an agent loop works internally | Show framework-based agent implementation |

---

# Demo Notes

For Task 1, show the manual cycle:

```text
Model request
Tool call detected
C# tool executed
Tool result returned
Final JSON parsed
```

For Task 2, show the MAF tool calls:

```text
[MAF TOOL EXECUTED] search_funny_episodes(30)
[MAF TOOL EXECUTED] get_title_details(office-stress-relief)
[MAF TOOL EXECUTED] add_to_watchlist(office-stress-relief, Tonight)
```

This demonstrates that the Microsoft Agent Framework agent can use registered C# tools with a local Ollama model.
