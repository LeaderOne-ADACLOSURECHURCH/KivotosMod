using System;using System.Collections.Generic;
namespace Microsoft.Xna.Framework {
 public struct Vector2 {public float X,Y;public Vector2(float x,float y){X=x;Y=y;}public static Vector2 operator +(Vector2 a,Vector2 b)=>new(a.X+b.X,a.Y+b.Y);public static Vector2 operator *(Vector2 a,float s)=>new(a.X*s,a.Y*s);}
 public struct Point {public int X,Y;public Point(int x,int y){X=x;Y=y;}}
 public struct Rectangle {public int X,Y,Width,Height;public int Right=>X+Width;public int Bottom=>Y+Height;public Point Center=>new(X+Width/2,Y+Height/2);public Rectangle(int x,int y,int w,int h){X=x;Y=y;Width=w;Height=h;}}
 public struct Color {public static Color White=>new();}
}
namespace Microsoft.Xna.Framework.Graphics {
 using Microsoft.Xna.Framework;
 public enum SpriteEffects{None}
 public class Texture2D {public int Width,Height;public string Name;public Rectangle Bounds=>new(0,0,Width,Height);public Texture2D(string name,int w,int h){Name=name;Width=w;Height=h;}}
 public class SpriteBatch {
  public readonly List<(string Name,Rectangle Rect)> Draws=new();
  public void Draw(Texture2D t,Rectangle r,Rectangle? source,Color c)=>Draws.Add((t.Name,r));
  public void Draw(Texture2D t,Vector2 position,Rectangle? source,Color c,float rotation,Vector2 origin,float scale,SpriteEffects effect,float depth){var s=source??t.Bounds;Draws.Add((t.Name,new Rectangle((int)MathF.Round(position.X-origin.X*scale),(int)MathF.Round(position.Y-origin.Y*scale),(int)MathF.Round(s.Width*scale),(int)MathF.Round(s.Height*scale))));}
 }
}
namespace ReLogic.Content {public class Asset<T>{public T Value;public Asset(T value){Value=value;}}}
