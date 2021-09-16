/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 * &
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

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// An animated property over time.
    /// For optimizations the curve is baked.
    /// </summary>
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
            int length = -1;
            if (null != cachedValues)
                length = cachedValues.Length - 1;
            ComputeCacheJob(0, length);
        }

        private void ComputeCacheJob(int startIndex, int endIndex)
        {
            if (null == cachedValues || cachedValues.Length != GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1)
            {
                cachedValues = new float[GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1];
                startIndex = 0;
                endIndex = cachedValues.Length - 1;
            }

            NativeArray<keyStruct> keysStructs = new NativeArray<keyStruct>(keys.Count, Allocator.TempJob);
            NativeArray<int> keysIndices = new NativeArray<int>(cachedKeysIndices, Allocator.TempJob);
            NativeArray<float> values = new NativeArray<float>(endIndex - startIndex, Allocator.TempJob);

            for (int i = 0; i < keys.Count; i++) keysStructs[i] = keyStruct.GetKeyStruct(keys[i]);

            CurveEvalutationJobs job = new CurveEvalutationJobs()
            {
                Keys = keysStructs,
                CachedKeysIndices = keysIndices,
                firstIndex = startIndex + GlobalState.Animation.StartFrame,
                Value = values
            };

            JobHandle jobHandle = job.Schedule(endIndex - startIndex, 20);
            jobHandle.Complete();

            for (int i = 0; i < (endIndex - startIndex); i++) cachedValues[i + startIndex] = values[i];

            keysStructs.Dispose();
            keysIndices.Dispose();
            values.Dispose();
        }

        public void ComputeCacheValuesAt(int keyIndex)
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

            ComputeCacheJob(start, end);
        }

        private void ComputeCacheIndices()
        {
            if (null == cachedKeysIndices || cachedKeysIndices.Length != GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1)
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

            if (keys.Count == 1)
            {
                int frame = keys[0].frame;
                if (frame <= GlobalState.Animation.EndFrame)
                    firstKeyFoundInRange = true;
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

        public bool GetKeyIndex(int frame, out int index)
        {
            index = cachedKeysIndices[frame - GlobalState.Animation.StartFrame];
            if (index == -1)
                return false;

            AnimationKey key = keys[index];
            return key.frame == frame;
        }

        public int GetPreviousKeyFrame(int frame)
        {
            int index = cachedKeysIndices[frame - GlobalState.Animation.StartFrame];
            if (index < 0) return 0;
            return keys[index].frame;
        }
        public int GetNextKeyFrame(int frame)
        {
            int index = cachedKeysIndices[frame - GlobalState.Animation.StartFrame] + 1;
            if (index >= keys.Count) return keys.Count - 1;
            return keys[index].frame;
        }

        public void SetKeys(List<AnimationKey> newKeys)
        {
            newKeys.ForEach(x => keys.Add(new AnimationKey(x)));
            ComputeCache();
        }

        public void RemoveKey(int frame, bool lockTangents = false)
        {
            if (GetKeyIndex(frame, out int index))
            {
                AnimationKey key = keys[index];
                int start = key.frame - GlobalState.Animation.StartFrame;
                int end = cachedKeysIndices.Length - 1;
                for (int i = start; i <= end; i++)
                    cachedKeysIndices[i]--;

                keys.RemoveAt(index);

                if (!lockTangents)
                    InitializeTangents(index);
                ComputeCacheValuesAt(index);
            }
        }

        // Don't compute cache. Should be called when adding a lot of keys in a row.
        // And then don't forget to call ComputeCache().
        public void AppendKey(AnimationKey key)
        {
            keys.Add(key);
        }

        public void AddKey(AnimationKey key, bool lockTangents = false)
        {
            if (GetKeyIndex(key.frame, out int index))
            {
                keys[index] = key;
                if (!lockTangents)
                {
                    PreviousTangent(index);
                    NextTangent(index);
                    if (key.inTangent == Vector2.zero && key.outTangent == Vector2.zero) CurrentTangent(index);
                }

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

                if (!lockTangents)
                {
                    PreviousTangent(index);
                    NextTangent(index);
                    if (key.inTangent == Vector2.zero && key.outTangent == Vector2.zero) CurrentTangent(index);
                }

                ComputeCacheValuesAt(index);
            }
        }


        private void PreviousTangent(int index)
        {
            index--;
            if (index > -1 && index < keys.Count)
            {
                if (index == 0 && index == keys.Count - 1)
                {
                    keys[index].outTangent = new Vector2();
                }
                else
                {
                    Vector2 key = new Vector2(keys[index].frame, keys[index].value);

                    if (index == 0)
                    {
                        Vector2 nextKey = new Vector2(keys[index + 1].frame, keys[index + 1].value);
                        keys[index].outTangent = (nextKey - key) / 3f;
                    }

                    else if (index == keys.Count - 1)
                    {
                        Vector2 prevKey = new Vector2(keys[index - 1].frame, keys[index - 1].value);
                        keys[index].outTangent = (key - prevKey) / 3f;
                    }

                    else
                    {
                        Vector2 prevKey = new Vector2(keys[index - 1].frame, keys[index - 1].value);
                        Vector2 nextKey = new Vector2(keys[index + 1].frame, keys[index + 1].value);
                        keys[index].outTangent = (nextKey - prevKey).normalized * ((nextKey.x - key.x) / 3f);
                    }
                }
            }
        }
        private void NextTangent(int index)
        {
            index++;
            if (index > -1 && index < keys.Count)
            {
                if (index == 0 && index == keys.Count - 1)
                {
                    keys[index].inTangent = new Vector2();
                }
                else
                {
                    Vector2 key = new Vector2(keys[index].frame, keys[index].value);

                    if (index == 0)
                    {
                        Vector2 nextKey = new Vector2(keys[index + 1].frame, keys[index + 1].value);
                        keys[index].inTangent = (nextKey - key) / 3f;
                    }

                    else if (index == keys.Count - 1)
                    {
                        Vector2 prevKey = new Vector2(keys[index - 1].frame, keys[index - 1].value);
                        keys[index].inTangent = (key - prevKey) / 3f;
                    }

                    else
                    {
                        Vector2 prevKey = new Vector2(keys[index - 1].frame, keys[index - 1].value);
                        Vector2 nextKey = new Vector2(keys[index + 1].frame, keys[index + 1].value);
                        keys[index].inTangent = (nextKey - prevKey).normalized * ((key.x - prevKey.x) / 3f);
                    }
                }
            }
        }

        private void CurrentTangent(int index)
        {
            if (index > -1 && index < keys.Count)
            {
                if (index == 0 && index == keys.Count - 1)
                {
                    keys[index].inTangent = new Vector2();
                    keys[index].outTangent = new Vector2();
                }
                else
                {
                    Vector2 key = new Vector2(keys[index].frame, keys[index].value);

                    if (index == 0)
                    {
                        Vector2 nextKey = new Vector2(keys[index + 1].frame, keys[index + 1].value);
                        keys[index].inTangent = (nextKey - key) / 3f;
                        keys[index].outTangent = (nextKey - key) / 3f;
                    }

                    else if (index == keys.Count - 1)
                    {
                        Vector2 prevKey = new Vector2(keys[index - 1].frame, keys[index - 1].value);
                        keys[index].inTangent = (key - prevKey) / 3f;
                        keys[index].outTangent = (key - prevKey) / 3f;
                    }

                    else
                    {
                        Vector2 prevKey = new Vector2(keys[index - 1].frame, keys[index - 1].value);
                        Vector2 nextKey = new Vector2(keys[index + 1].frame, keys[index + 1].value);
                        keys[index].inTangent = (nextKey - prevKey).normalized * ((key.x - prevKey.x) / 3f);
                        keys[index].outTangent = (nextKey - prevKey).normalized * ((nextKey.x - key.x) / 3f);
                    }
                }
            }
        }

        private void InitializeTangents(int index)
        {
            PreviousTangent(index);
            CurrentTangent(index);
            NextTangent(index);
        }


        /// <summary>
        /// Add a key at startFrame and endFrame. Move every keys in between towards the new key value. Then add the new keyframe.
        /// Used when draging a curve.
        /// </summary>
        public void AddZoneKey(AnimationKey key, int startFrame, int endFrame)
        {
            int firstKeyIndex = cachedKeysIndices[startFrame - (GlobalState.Animation.StartFrame - 1)];
            int lastKeyIndex = cachedKeysIndices[endFrame - (GlobalState.Animation.StartFrame - 1)];

            if (!Evaluate(key.frame, out float value)) return;
            if (keys[firstKeyIndex].frame != startFrame && Evaluate(startFrame, out float prevValue))
            {
                AddKey(new AnimationKey(startFrame, prevValue, key.interpolation));
            }
            if (keys[lastKeyIndex].frame != endFrame && Evaluate(endFrame, out float nextValue))
            {
                AddKey(new AnimationKey(endFrame, nextValue, key.interpolation));
            }

            float deltaValue = key.value - value;
            float zoneSize = endFrame - startFrame;
            for (int i = firstKeyIndex; i <= lastKeyIndex; i++)
            {
                int deltaFrame = Mathf.Abs(key.frame - keys[i].frame);
                float deltaTime = 1 - (deltaFrame / (float)zoneSize);

                if (property == AnimatableProperty.RotationX || property == AnimatableProperty.RotationY || property == AnimatableProperty.RotationZ)
                {
                    keys[i].value = Mathf.LerpAngle(keys[i].value, keys[i].value + deltaValue, deltaTime);
                }
                else
                {
                    keys[i].value = Mathf.Lerp(keys[i].value, keys[i].value + deltaValue, deltaTime);
                }
                ComputeCacheValuesAt(i);
            }
            AddKey(key);
        }

        /// <summary>
        /// Same as AddZoneKey, but fill oldKeys and newKeys instead of modifying the curve.
        /// Used when the curve is released, to apply changes through commands
        /// </summary>
        public void GetZoneKeyChanges(AnimationKey key, int startFrame, int endFrame, List<AnimationKey> oldKeys, List<AnimationKey> newKeys)
        {
            int firstKeyIndex = cachedKeysIndices[startFrame - (GlobalState.Animation.StartFrame - 1)];
            int lastKeyIndex = cachedKeysIndices[endFrame - (GlobalState.Animation.StartFrame - 1)];

            if (!Evaluate(key.frame, out float value)) return;
            if (keys[firstKeyIndex].frame != startFrame && Evaluate(startFrame, out float prevValue))
            {
                newKeys.Add(new AnimationKey(startFrame, prevValue, key.interpolation));
            }
            if (keys[lastKeyIndex].frame != endFrame && Evaluate(endFrame, out float nextValue))
            {
                newKeys.Add(new AnimationKey(endFrame, nextValue, key.interpolation));
            }

            float deltaValue = key.value - value;
            float zoneSize = endFrame - startFrame;
            for (int i = firstKeyIndex; i <= lastKeyIndex; i++)
            {
                int deltaFrame = Mathf.Abs(key.frame - keys[i].frame);
                float deltaTime = 1 - (deltaFrame / (float)zoneSize);

                if (property == AnimatableProperty.RotationX || property == AnimatableProperty.RotationY || property == AnimatableProperty.RotationZ)
                {
                    oldKeys.Add(new AnimationKey(keys[i].frame, keys[i].value, keys[i].interpolation));
                    float newValue = Mathf.LerpAngle(keys[i].value, keys[i].value + deltaValue, deltaTime);
                    newKeys.Add(new AnimationKey(keys[i].frame, newValue, key.interpolation));
                }
                else
                {
                    oldKeys.Add(new AnimationKey(keys[i].frame, keys[i].value, keys[i].interpolation));
                    float newValue = Mathf.Lerp(keys[i].value, keys[i].value + deltaValue, deltaTime);
                    newKeys.Add(new AnimationKey(keys[i].frame, newValue, key.interpolation));
                }
            }
            if (TryFindKey(key.frame, out AnimationKey oldKey)) oldKeys.Add(new AnimationKey(oldKey.frame, oldKey.value, oldKey.interpolation));
            newKeys.Add(new AnimationKey(key.frame, key.value, key.interpolation));
        }

        /// <summary>
        /// Adds a keyframe at start and at end. Remove all keyframes in between.
        /// Used when starting to drag a curve to modify tangents.
        /// </summary>
        public void AddTangentKey(AnimationKey key, int start, int end)
        {
            if (keys.Count == 0) return;

            int startFrame = Mathf.Max(GlobalState.Animation.StartFrame, start);
            int endFrame = Mathf.Min(GlobalState.Animation.EndFrame, end);

            int firstKeyIndex = Mathf.Max(cachedKeysIndices[startFrame - (GlobalState.Animation.StartFrame)], 0);
            int lastKeyIndex = Mathf.Max(cachedKeysIndices[endFrame - (GlobalState.Animation.StartFrame)], 0);

            bool hasPrevValue = Evaluate(startFrame, out float prevValue);
            bool hasNextValue = Evaluate(endFrame, out float nextValue);

            if (keys[firstKeyIndex].frame != startFrame && hasPrevValue)
            {
                AddKey(new AnimationKey(startFrame, prevValue, Interpolation.Bezier), false);
            }
            if (keys[lastKeyIndex].frame != endFrame && hasNextValue)
            {
                AddKey(new AnimationKey(endFrame, nextValue, Interpolation.Bezier), false);
            }
            List<AnimationKey> toRemove = keys.FindAll(x => x.frame > startFrame && x.frame < endFrame);
            toRemove.ForEach(x => RemoveKey(x.frame));
        }

        /// <summary>
        /// fills oldKeys with all previous keyframes between startFrame and endFrame.
        /// Used when releasing a curve, to apply changes through commands
        /// </summary>
        public void GetTangentKeys(int startFrame, int endFrame, ref List<AnimationKey> oldKeys)
        {
            oldKeys = keys.FindAll(x => x.frame >= startFrame && x.frame <= endFrame);
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

        public AnimationKey GetNextKey(int frame)
        {
            frame -= GlobalState.Animation.StartFrame;
            if (frame >= 0 && frame < cachedKeysIndices.Length)
            {
                int index = cachedKeysIndices[frame];
                if (index + 1 < keys.Count)
                {
                    return keys[index + 1];
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

        public void SetTangents(int index, Vector2 inTangent, Vector2 outTangent)
        {
            keys[index].inTangent = inTangent;
            keys[index].outTangent = outTangent;
            ComputeCacheValuesAt(index);
        }
    }
}
