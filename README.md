# Random Generator for Unity

A Unity package for generating random maps efficiently. This tool is designed to create hundreds of randomly generated maps for games, with an emphasis on performance and flexibility.

## Features

- **Multithreaded Map Generation:** Generate maps using multiple threads to optimize performance for large-scale map generation.
- **Configurable Parameters:** Customize map size, tile types, and seed for generation to create diverse environments.
- **Serializable Parameters:** 
- **Dynamic Algorithm Stack:** Customize which algorithms you want to run in which order
- **Visualizer Compatibility:** Works with a random generator visualizer that will eventually export parameter files for use in unity projects, this visualizer also has a built-in testing framework
- **Texture Packing:** Support for packing multiple tilemaps into textures for efficient rendering.

## Installation

1. Clone this repository or download the zip file.
2. Add the package to your Unity project by copying the `RandomGenerator` folder into your `Packages` directory.
3. Open Unity and go to **Assets > Import Package > Custom Package** to import it into your project.

## Getting Started

1. Add the `MapGenerator` component to any GameObject in your scene.
2. Set your map parameters (size, tile types, etc.) in the Inspector.
3. Call `GenerateMap()` from a script or set it to auto-generate in the editor.

### Example Usage

```csharp
using UnityEngine;
using RandomMapGenerator;

public class Example : MonoBehaviour
{
    public MapGenerator mapGenerator;

    void Start()
    {
        mapGenerator.GenerateMap();
    }
}
