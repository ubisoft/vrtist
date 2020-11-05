using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class Curve
    {
        public AnimatableProperty property;
        public List<AnimationKey> keys;
        private int[] cachedKeysIndices;
        private float[] cachedValues;

        public Curve(AnimatableProperty property)
        {
            this.property = property;
            keys = new List<AnimationKey>();
            cachedKeysIndices = new int[GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1];
            for (int i = 0; i < cachedKeysIndices.Length; i++)
                cachedKeysIndices[i] = -1;
            cachedValues = new float[GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1];
        }

        public void ClearCache()
        {
            cachedKeysIndices = null;
            cachedValues = null;
        }

        public void ComputeCache()
        {
            ComputeCacheIndices();
            ComputeCacheValues(0, cachedValues.Length - 1);
        }

        private void ComputeCacheValues(int startIndex, int endIndex)
        {
            if (cachedValues.Length != GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1)
            {
                cachedValues = new float[GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1];
                startIndex = 0;
                endIndex = cachedValues.Length - 1;
            }

            for (int i = startIndex; i <= endIndex; i++)
            {
                _Evaluate(i + GlobalState.Animation.StartFrame, out cachedValues[i]);
            }
        }

        private void ComputeCacheValuesAt(int keyIndex)
        {
            // recompute value cache in range [index - 2 ; index + 2] (for bezier curves)            
            int startKeyIndex = keyIndex - 2;
            int endKeyIndex = keyIndex + 2;

            int start = 0;
            if (startKeyIndex >= 0 && startKeyIndex <= keys.Count - 1)
                start = Mathf.Clamp(keys[startKeyIndex].frame - GlobalState.Animation.StartFrame, 0, cachedValues.Length - 1);

            int end = cachedValues.Length - 1;
            if (endKeyIndex >= 0 && endKeyIndex <= keys.Count - 1)
                end = Mathf.Clamp(keys[endKeyIndex].frame - GlobalState.Animation.StartFrame, 0, cachedValues.Length - 1);

            ComputeCacheValues(start, end);
        }

        private void ComputeCacheIndices()
        {
            if (cachedKeysIndices.Length != GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1)
            {
                cachedKeysIndices = new int[GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1];
            }

            if (keys.Count == 0)
            {
                for (int i = 0; i < cachedKeysIndices.Length; i++)
                    cachedKeysIndices[i] = -1;

                return;
            }

            bool firstKeyFoundInRange = false;
            int lastKeyIndex = 0;
            for (int i = 0; i < keys.Count - 1; i++)
            {
                float keyTime = keys[i].frame;
                if (keyTime < GlobalState.Animation.StartFrame || keyTime > GlobalState.Animation.EndFrame)
                    continue;

                int b1 = keys[i].frame - GlobalState.Animation.StartFrame;
                int b2 = keys[i + 1].frame - GlobalState.Animation.StartFrame;
                b2 = Mathf.Clamp(b2, b1, cachedKeysIndices.Length);

                if (!firstKeyFoundInRange) // Fill framedKeys from 0 to first key
                {
                    for (int j = 0; j < b1; j++)
                    {
                        cachedKeysIndices[j] = i - 1;
                    }
                    firstKeyFoundInRange = true;
                }

                for (int j = b1; j < b2; j++)
                    cachedKeysIndices[j] = i;
                lastKeyIndex = i;
            }

            // found no key in range
            if (!firstKeyFoundInRange)
            {
                int index = -1;
                if (keys[keys.Count - 1].frame < GlobalState.Animation.StartFrame)
                    index = keys.Count - 1;
                for (int i = 0; i < cachedKeysIndices.Length; i++)
                    cachedKeysIndices[i] = index;
                return;
            }

            // fill framedKey from last key found to end
            lastKeyIndex++;
            lastKeyIndex = Math.Min(lastKeyIndex, keys.Count - 1);
            int jmin = Math.Max(0, keys[lastKeyIndex].frame - GlobalState.Animation.StartFrame);
            for (int j = jmin; j < cachedKeysIndices.Length; j++)
            {
                cachedKeysIndices[j] = lastKeyIndex;
            }
        }

        private bool GetKeyIndex(int frame, out int index)
        {
            index = cachedKeysIndices[frame - GlobalState.Animation.StartFrame];
            if (index == -1)
                return false;

            AnimationKey key = keys[index];
            return key.frame == frame;
        }

        public void SetKeys(List<AnimationKey> k)
        {
            keys = k;
            ComputeCache();
        }

        public void RemoveKey(int frame)
        {
            if (GetKeyIndex(frame, out int index))
            {
                AnimationKey key = keys[index];
                int start = key.frame - GlobalState.Animation.StartFrame;
                int end = cachedKeysIndices.Length - 1;
                for (int i = start; i <= end; i++)
                    cachedKeysIndices[i]--;

                keys.RemoveAt(index);

                ComputeCacheValuesAt(index);
            }
        }

        // Don't compute cache. Should be called when adding a lot of keys in a row.
        // And then don't forget to call ComputeCache().
        public void AppendKey(AnimationKey key)
        {
            keys.Add(key);
        }

        public void AddKey(AnimationKey key)
        {
            if (GetKeyIndex(key.frame, out int index))
            {
                keys[index] = key;
                ComputeCacheValuesAt(index);
            }
            else
            {
                index++;
                keys.Insert(index, key);

                int end = cachedKeysIndices.Length - 1;
                if (index + 1 < keys.Count)
                {
                    end = keys[index + 1].frame - GlobalState.Animation.StartFrame - 1;
                    end = Mathf.Clamp(end, 0, cachedKeysIndices.Length - 1);
                }

                int start = key.frame - GlobalState.Animation.StartFrame;
                start = Mathf.Clamp(start, 0, end);
                for (int i = start; i <= end; i++)
                    cachedKeysIndices[i] = index;
                for (int i = end + 1; i < cachedKeysIndices.Length; i++)
                    cachedKeysIndices[i]++;

                ComputeCacheValuesAt(index);
            }
        }

        public void MoveKey(int oldFrame, int newFrame)
        {
            if (GetKeyIndex(oldFrame, out int index))
            {
                AnimationKey key = keys[index];
                RemoveKey(key.frame);
                key.frame = newFrame;
                AddKey(key);
            }
        }

        public AnimationKey GetKey(int index)
        {
            return keys[index];
        }

        public AnimationKey GetPreviousKey(int frame)
        {
            --frame;
            frame -= GlobalState.Animation.StartFrame;
            if (frame >= 0 && frame < cachedKeysIndices.Length)
            {
                int index = cachedKeysIndices[frame];
                if (index != -1)
                {
                    return keys[index];
                }
            }
            return null;
        }

        public bool HasKeyAt(int frame)
        {
            foreach (var key in keys)
            {
                if (key.frame == frame) { return true; }
            }
            return false;
        }

        public bool TryFindKey(int frame, out AnimationKey key)
        {
            if (GetKeyIndex(frame, out int index))
            {
                key = keys[index];
                return true;
            }
            key = null;
            return false;
        }

        public bool Evaluate(int frame, out float value)
        {
            if (keys.Count == 0)
            {
                value = float.NaN;
                return false;
            }

            value = cachedValues[frame - GlobalState.Animation.StartFrame];
            return value != float.NaN;
        }

        private Vector2 CubicBezier(Vector2 A, Vector2 B, Vector2 C, Vector2 D, float t)
        {
            float invT1 = 1 - t;
            float invT2 = invT1 * invT1;
            float invT3 = invT2 * invT1;

            float t2 = t * t;
            float t3 = t2 * t;

            return (A * invT3) + (B * 3 * t * invT2) + (C * 3 * invT1 * t2) + (D * t3);
        }

        private float EvaluateBezier(Vector2 A, Vector2 B, Vector2 C, Vector2 D, int frame)
        {
            if ((float)frame == A.x)
                return A.y;

            if ((float)frame == D.x)
                return D.y;

            float pmin = 0;
            float pmax = 1;
            Vector2 avg = A;
            float dt = D.x - A.x;
            while (dt > 0.1f)
            {
                float param = (pmin + pmax) * 0.5f;
                avg = CubicBezier(A, B, C, D, param);
                if (avg.x < frame)
                {
                    pmin = param;
                }
                else
                {
                    pmax = param;
                }
                dt = Math.Abs(avg.x - (float)frame);
            }
            return avg.y;
        }

        public bool _Evaluate(int frame, out float value)
        {
            if (keys.Count == 0)
            {
                value = float.NaN;
                return false;
            }

            int prevIndex = cachedKeysIndices[frame - GlobalState.Animation.StartFrame];
            if (prevIndex == -1)
            {
                value = keys[0].value;
                return true;
            }
            if (prevIndex == keys.Count - 1)
            {
                value = keys[keys.Count - 1].value;
                return true;
            }

            AnimationKey prevKey = keys[prevIndex];
            switch (prevKey.interpolation)
            {
                case Interpolation.Constant:
                    value = prevKey.value;
                    return true;

                case Interpolation.Linear:
                    {
                        AnimationKey nextKey = keys[prevIndex + 1];
                        float dt = (float)(frame - prevKey.frame) / (float)(nextKey.frame - prevKey.frame);
                        float oneMinusDt = 1f - dt;
                        value = prevKey.value * oneMinusDt + nextKey.value * dt;
                        return true;
                    }

                case Interpolation.Bezier:
                    {
                        AnimationKey nextKey = keys[prevIndex + 1];
                        float rangeDt = (float)(nextKey.frame - prevKey.frame);

                        Vector2 A = new Vector2(prevKey.frame, prevKey.value);
                        Vector2 B, C;
                        Vector2 D = new Vector2(nextKey.frame, nextKey.value);

                        if (prevIndex == 0)
                        {
                            B = A + (D - A) / 3f;
                        }
                        else
                        {
                            AnimationKey prevPrevKey = keys[prevIndex - 1];
                            Vector2 V = (D - new Vector2(prevPrevKey.frame, prevPrevKey.value)).normalized;
                            Vector2 AD = D - A;
                            B = A + V * AD.magnitude / 3f;
                        }

                        if (prevIndex + 2 >= keys.Count)
                        {
                            C = D - (D - A) / 3f;
                        }
                        else
                        {
                            AnimationKey nextNextKey = keys[prevIndex + 2];
                            Vector2 V = (new Vector2(nextNextKey.frame, nextNextKey.value) - A).normalized;
                            Vector2 AD = D - A;
                            C = D - V * AD.magnitude / 3f;
                        }

                        //float dt = (float) (frame - prevKey.frame) / rangeDt;
                        value = EvaluateBezier(A, B, C, D, frame);
                        return true;
                    }

            }
            value = float.NaN;
            return false;
        }
    }
}
