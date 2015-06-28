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
            float xMin = target.x + min - pos.width;
            float xMax = target.xMax - min;
            float yMin = target.y + min - pos.height;
            float yMax = target.yMax - min;

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
            float xMin = target.x;
            float xMax = target.xMax - pos.width;
            float yMin = target.y;
            float yMax = target.yMax - pos.height;

            pos.x = Mathf.Clamp(pos.x, xMin, xMax);
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);

            return pos;
        }

        public static Rect ClampToScreenEdge(Rect pos)
        {
            return ClampToRectEdge(pos, ScreenRect());
        }

        public static Rect ClampToRectEdge(Rect pos, Rect target)
        {
            float topSeparation = Math.Abs(target.y - pos.y);
            float bottomSeparation = Math.Abs(target.yMax - pos.yMax);
            float leftSeparation = Math.Abs(target.x - pos.x);
            float rightSeparation = Math.Abs(target.xMax - pos.xMax);

            if (topSeparation <= bottomSeparation && topSeparation <= leftSeparation && topSeparation <= rightSeparation)
            {
                pos.y = target.y;
            }
            else if (leftSeparation <= topSeparation && leftSeparation <= bottomSeparation &&
                leftSeparation <= rightSeparation)
            {
                pos.x = target.x;
            }
            else if (bottomSeparation <= topSeparation && bottomSeparation <= leftSeparation &&
                bottomSeparation <= rightSeparation)
            {
                pos.y = target.yMax - pos.height;
            }
            else if (rightSeparation <= topSeparation && rightSeparation <= bottomSeparation &&
                rightSeparation <= leftSeparation)
            {
                pos.x = target.xMax - pos.width;
            }

            return pos;
        }

        public static Rect ClampToScreenAngle(Rect pos)
        {
            return ClampToRectAngle(pos, ScreenRect());
        }

        public static Rect ClampToRectAngle(Rect pos, Rect target)
        {
            float topSeparation = Math.Abs(target.y - pos.y);
            float bottomSeparation = Math.Abs(target.yMax - pos.yMax);
            float leftSeparation = Math.Abs(target.x - pos.x);
            float rightSeparation = Math.Abs(target.xMax - pos.xMax);

            if (topSeparation <= bottomSeparation) {
                pos.y = target.y;
            } 
            else 
            {
                pos.y = target.yMax - pos.height;
            }
            if (leftSeparation <= rightSeparation)
            {
                pos.x = target.x;
            }
            else
            {
                pos.x = target.xMax - pos.width;
            }

            return pos;
        }
    }
}
