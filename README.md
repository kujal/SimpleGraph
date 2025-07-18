# SimpleGraph Documentation

SimpleGraph is a set of Unity scripts that simplifies the process of building both linear and non-linear task trees. It features a user-friendly, node-based interface that makes it easy to create logic-driven workflows. 

## Table of Contents
- [Getting Started](#getting-started)
- [Installation](#installation)
- [Create a New Graph](#create-a-new-graph)
- [Create Node Logic](#create-node-logic)
- [Usage](#usage)
  - [Creating and Managing Nodes](#creating-and-managing-nodes)
  - [Completing or Uncompleting Nodes](#completing-or-uncompleting-nodes)
  - [Triggering Nodes and UnityEvents](#triggering-nodes-and-unityevents)
- [Nodes](#nodes)
- [Triggers](#triggers)
- [Workflow](#workflow)
- [Integration to the Cloud](#integration-to-the-cloud)


## Installation

### Add to Unity Project
Place the `SimpleGraph` folder inside the `Assets` folder of your Unity project.


## Create a New Graph

### 1. Create an Empty GameObject
Create a new GameObject in the Unity hierarchy.

### 2. Attach the `GraphManager` Script
Drag and drop the `GraphManager` script onto the GameObject or use the "Add Component" button in the Inspector to attach it.

### 3. Edit the Graph
In the GameObject’s Inspector window, click the **Edit Graph** button to start creating your graph.


## Create Node Logic

### 1. Add a Node
- Inside the Graph Window, right-click to open the context menu.
- Use the menu to add a new node.

### 2. Create Connections Between Nodes
- Click the right side of the node you want to connect from.
- Then, click the left side of the node you want to connect to. This will establish a connection.

### 3. Troubleshooting Connections
In rare cases, connections may not register immediately. If this happens, simply try the process again.


## Usage

### Creating and Managing Nodes
When you create a node in the Graph Window, a corresponding physical GameObject is automatically created in Unity’s hierarchy.

**Important:**
- Do not manually move the nodes away from their parent object.
- Avoid adding or relocating nodes within the hierarchy.

### Completing or Uncompleting Nodes

#### Using UnityEvents
To complete or uncomplete nodes, use the UnityEvents system.

#### Built-in Triggers
For example, a **Timer Trigger** can be used to manage node completion.

**Steps to Complete a Node:**
1. Open the UnityEvents window and add a new event.
2. Attach the “root” node (e.g., Example Exercise) to the event.
3. Select the `CompleteNode` function.
4. In the box that appears, drag and drop the task you wish to mark as complete.
5. When the UnityEvent is triggered, the selected node will be marked as completed.


### Triggering Nodes and UnityEvents

#### `onActiveElement` Trigger:
This event is triggered automatically when all immediate previous nodes connected to the current node have been completed.

#### `onCompletionEvent`:
This event is only triggered when `GraphManager.CompleteNode` is explicitly called for that node.


## Nodes

| **Node Type**  | **Functionality**                                                                                                                                 |
|----------------|---------------------------------------------------------------------------------------------------------------------------------------------------|
| **StartNode**  | Should be the first node of a logic tree. When completed, it does not check for previous nodes.                                                  |
| **TaskNode**   | Activates automatically when the immediate previous nodes have been completed. Needs to be completed manually.                                    |
| **InverterNode**| “Inverts” the state of the node it has been attached to. Acts as an “inverter” and can only be attached to a single node. Completes automatically.|
| **EndNode**    | The last node of a node tree. Should be used to finish an exercise.                                                                                |

## Triggers

| **Trigger Type** | **Functionality**                                                        |
|-------------------|--------------------------------------------------------------------------|
| **Timer Trigger** | Triggers a UnityEvent after a specific time period. Also handy for debugging purposes. |

