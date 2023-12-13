using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class SimpleMovingAverageRotation
{
    // https://andrewlock.net/creating-a-simple-moving-average-calculator-in-csharp-1-a-simple-moving-average-calculator/

    private readonly int _k;
    private Quaternion[] _values;

    private int _index = 0;

    public SimpleMovingAverageRotation(int k)
    {
        if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), "Must be greater than 0");

        _k = k;
        _values = new Quaternion[k];
    }

    public void PopulateValues(Quaternion value)
    {
        for (int i = 0; i < _values.Length; i++)
        {
            _values[i] = value;
        }
    }

    public Quaternion Step(List<Quaternion> nextInput)
    {
        // overwrite the old value with the new one
        //_values[_index] = nextInput;

        Quaternion quat = CalculateAverageQuaternion();

        // increment the index (wrapping back to 0)
        _index = (_index + 1) % _k;

        // calculate the average
        return quat;
    }

    Quaternion CalculateAverageQuaternion()
    {
        // https://gamedev.stackexchange.com/questions/119688/calculate-average-of-arbitrary-amount-of-quaternions-recursion
        float x = 0, y = 0, z = 0, w = 0;

        foreach (Quaternion quat in _values)
        {
            x += quat.x;
            y += quat.y;
            z += quat.z;
            w += quat.w;
        }

        float k = 1.0f / Mathf.Sqrt(x * x + y * y + z * z + w * w);

        if (k > 1)
        {
            return _values[_index];
        }

        return new Quaternion(x * k, y * k, z * k, w * k);
    }
}