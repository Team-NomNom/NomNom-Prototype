using System;
using UnityEngine;

public class Colin
{
    int count = 0;
    public void Main()
    {
        // Recursion weeeeee
        Console.WriteLine(count);
        count++;
        Main();

    }
}
