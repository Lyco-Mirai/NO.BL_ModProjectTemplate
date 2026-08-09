using Microsoft.VisualBasic;
using System; 
using UnityEngine;

namespace Rewired.Glyphs
{
    namespace UnityUI
    {
        public class UnityUITextMeshProGlyphHelper
        {
            public string text;
        }
    }
}
namespace Rewired.UI
{
    public class Dummy { } 
    namespace ControlMapper
    {
        public class ControlMapper
        {
            public void Open(){}
            public event Action ScreenClosedEvent;
        } 
    }
}
namespace JamesFrowen.Mirage
{
    public class Dummy { } 
    
    namespace DebugScripts
    {
        public class LagSocketFactory
        {
            public void inner(params object[] argument){}
        } 
    }
}
namespace GameHelper
{
    public class Dummy { }
}
namespace BL.NO_Patches
{
    public class TobiiAPI
    {
        public static void ApplyTobiiSettings(params object[] argument){}
    }
}

public class IHeadTracker
{
    public System.Tuple<UnityEngine.Vector3, UnityEngine.Quaternion> GetHeadTrackerOffset(params object[] argument){ return System.Tuple.Create(new UnityEngine.Vector3(), new UnityEngine.Quaternion()); }
    public void Recenter(){}
}
public class TrackIRComponent
{
    public static IHeadTracker i;
}