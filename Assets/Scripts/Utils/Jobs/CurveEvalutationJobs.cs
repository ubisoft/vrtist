/* MIT License
 *
 * Université de Rennes 1 / Invictus Project
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VRtist;
using Unity.Burst;


[BurstCompile(CompileSynchronously = true)]
public struct CurveEvalutationJobs : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<keyStruct> Keys;
    [ReadOnly]
    public NativeArray<int> CachedKeysIndices;
    [ReadOnly]
    public int firstIndex;
    [WriteOnly]
    public NativeArray<float> Value;

    public void Execute(int index)
    {
        if (Keys.Length == 0)
        {
            Value[index] = 0;
            return;
        }
        int prevIndex = CachedKeysIndices[index + firstIndex - 1];
        if (prevIndex == -1)
        {
            Value[index] = Keys[0].value;
            return;
        }
        if (prevIndex == Keys.Length - 1)
        {
            Value[index] = Keys[Keys.Length - 1].value;
            return;
        }

        keyStruct prevKey = Keys[prevIndex];
        switch (prevKey.interpolation)
        {
            case Interpolation.Constant:
                Value[index] = prevKey.value;
                break;
            case Interpolation.Linear:
                keyStruct nextKey = Keys[prevIndex + 1];
                float dt = (index - prevKey.frame) / (float)(nextKey.frame - prevKey.frame);
                float oneMinusDt = 1f - dt;
                Value[index] = prevKey.value * oneMinusDt + nextKey.value * dt;
                break;
            case Interpolation.Bezier:
                keyStruct nextKey1 = Keys[prevIndex + 1];
                Vector2 A = new Vector2(prevKey.frame, prevKey.value);
                Vector2 D = new Vector2(nextKey1.frame, nextKey1.value);

                Vector2 B = A + prevKey.outTangent;
                Vector2 C = D - nextKey1.inTangent;
                Value[index] = Bezier.EvaluateBezier(A, B, C, D, index + firstIndex);

                break;
        }
    }
}

public struct keyStruct
{
    public int frame;
    public float value;
    public Vector2 inTangent;
    public Vector2 outTangent;
    public Interpolation interpolation;

    public static keyStruct GetKeyStruct(AnimationKey key)
    {
        return new keyStruct()
        {
            frame = key.frame,
            value = key.value,
            inTangent = key.inTangent,
            outTangent = key.outTangent,
            interpolation = key.interpolation
        };
    }
}
