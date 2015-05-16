// this file is based on RectExtensions.cs from InfernalRobotics

using System;
using UnityEngine;

namespace kOS.Screen
{
    public static class RectExtensions
    {
        public static Rect ScreenRect()
        {
            return new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height);
        }

        public static Rect EnsureVisible(Rect pos, float min = 16.0f)
        {
            return EnsurePartiallyInside(pos, ScreenRect(), min);
        }

        public static Rect EnsurePartiallyInside(Rect pos, Rect target, float min = 16.0f)
        {
            float xMin = min - pos.width;
            float xMax = target.width - min;
            float yMin = min - pos.height;
            float yMax = target.height - min;

            pos.x = Mathf.Clamp(pos.x, xMin, xMax);
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);

            return pos;
        }

        public static Rect EnsureCompletelyVisible(Rect pos)
        {
            return EnsureCompletelyInside(pos, ScreenRect());
        }

        public static Rect EnsureCompletelyInside(Rect pos, Rect target)
        {
            const float X_MIN = 0;
            float xMax = target.width - pos.width;
            const float Y_MIN = 0;
            float yMax = target.height - pos.height;

            pos.x = Mathf.Clamp(pos.x, X_MIN, xMax);
            pos.y = Mathf.Clamp(pos.y, Y_MIN, yMax);

            return pos;
        }

        public static Rect ClampToScreenEdge(Rect pos)
        {
            return ClampToRectEdge(pos, ScreenRect());
        }

        public static Rect ClampToRectEdge(Rect pos, Rect target)
        {
            float topSeparation = Math.Abs(pos.y);
            float bottomSeparation = Math.Abs(target.height - pos.y - pos.height);
            float leftSeparation = Math.Abs(pos.x);
            float rightSeparation = Math.Abs(target.width - pos.x - pos.width);

            if (topSeparation <= bottomSeparation && topSeparation <= leftSeparation && topSeparation <= rightSeparation)
            {
                pos.y = 0;
            }
            else if (leftSeparation <= topSeparation && leftSeparation <= bottomSeparation &&
                leftSeparation <= rightSeparation)
            {
                pos.x = 0;
            }
            else if (bottomSeparation <= topSeparation && bottomSeparation <= leftSeparation &&
                bottomSeparation <= rightSeparation)
            {
                pos.y = target.height - pos.height;
            }
            else if (rightSeparation <= topSeparation && rightSeparation <= bottomSeparation &&
                rightSeparation <= leftSeparation)
            {
                pos.x = target.width - pos.width;
            }

            return pos;
        }

        public static Rect ClampToScreenAngle(Rect pos)
        {
            return ClampToRectAngle(pos, ScreenRect());
        }

        public static Rect ClampToRectAngle(Rect pos, Rect target)
        {
            float topSeparation = Math.Abs(pos.y);
            float bottomSeparation = Math.Abs(target.height - pos.y - pos.height);
            float leftSeparation = Math.Abs(pos.x);
            float rightSeparation = Math.Abs(target.width - pos.x - pos.width);

            if (topSeparation <= bottomSeparation) {
                pos.y = 0;
            } 
            else 
            {
                pos.y = target.height - pos.height;
            }
            if (leftSeparation <= rightSeparation)
            {
                pos.x = 0;
            }
            else
            {
                pos.x = target.width - pos.width;
            }

            return pos;
        }
    }
}
