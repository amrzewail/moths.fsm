using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FSMNodeAttribute : System.Attribute
{
    public string path { get; private set; }

    public FSMNodeAttribute(string path)
    {
        this.path = path;
    }
}
