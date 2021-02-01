using System.Runtime.Serialization;

using UnityEngine;

namespace VRtist.Serialization
{
    public class Vector2Surrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Vector2 v = (Vector2)obj;
            info.AddValue("x", v.x);
            info.AddValue("y", v.y);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Vector2 v = (Vector2)obj;
            v.x = (float)info.GetValue("x", typeof(float));
            v.y = (float)info.GetValue("y", typeof(float));
            obj = v;
            return obj;
        }
    }


    public class Vector3Surrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Vector3 v = (Vector3)obj;
            info.AddValue("x", v.x);
            info.AddValue("y", v.y);
            info.AddValue("z", v.z);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Vector3 v = (Vector3)obj;
            v.x = (float)info.GetValue("x", typeof(float));
            v.y = (float)info.GetValue("y", typeof(float));
            v.z = (float)info.GetValue("z", typeof(float));
            obj = v;
            return obj;
        }
    }


    public class Vector4Surrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Vector4 v = (Vector4)obj;
            info.AddValue("x", v.x);
            info.AddValue("y", v.y);
            info.AddValue("z", v.z);
            info.AddValue("w", v.w);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Vector4 v = (Vector4)obj;
            v.x = (float)info.GetValue("x", typeof(float));
            v.y = (float)info.GetValue("y", typeof(float));
            v.z = (float)info.GetValue("z", typeof(float));
            v.w = (float)info.GetValue("w", typeof(float));
            obj = v;
            return obj;
        }
    }


    public class QuaternionSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Quaternion q = (Quaternion)obj;
            info.AddValue("x", q.x);
            info.AddValue("y", q.y);
            info.AddValue("z", q.z);
            info.AddValue("w", q.w);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Quaternion q = (Quaternion)obj;
            q.x = (float)info.GetValue("x", typeof(float));
            q.y = (float)info.GetValue("y", typeof(float));
            q.z = (float)info.GetValue("z", typeof(float));
            q.w = (float)info.GetValue("w", typeof(float));
            obj = q;
            return obj;
        }
    }


    public class ColorSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Color c = (Color)obj;
            info.AddValue("r", c.r);
            info.AddValue("g", c.g);
            info.AddValue("b", c.b);
            info.AddValue("a", c.a);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Color c = (Color)obj;
            c.r = (float)info.GetValue("r", typeof(float));
            c.g = (float)info.GetValue("g", typeof(float));
            c.b = (float)info.GetValue("b", typeof(float));
            c.a = (float)info.GetValue("a", typeof(float));
            obj = c;
            return obj;
        }
    }
}
