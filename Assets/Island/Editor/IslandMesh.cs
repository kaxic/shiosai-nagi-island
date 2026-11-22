using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Each face carries its own paint and normal. All the pieces of a model share one draw call.
internal sealed class IslandMesh
{
    // The regular UVs remain usable with another material. UV3.x selects a paint in IslandPaint.
    internal enum Surface { Auto=-1, Plain, Grass, Sand, Sea, Timber, Leaves, Stone, Path, Foam, Roof, Soil, Cloth, Ember }
    internal Surface surface = Surface.Auto;
    bool grainRunsUp;
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Vector3> normals = new List<Vector3>();
    readonly List<Color32> colors = new List<Color32>();
    readonly List<int> triangles = new List<int>();
    readonly List<Vector2> uvs = new List<Vector2>();
    readonly List<Vector2> paintLayers = new List<Vector2>();

    static Surface Pigment(Color c)
    {
        if (c.g>c.r*1.08f && c.g>c.b*1.19f) return Surface.Leaves;
        if (c.r>c.b*1.49f && c.g>c.b*1.16f && c.r>c.g*1.13f) return Surface.Timber;
        if (c.b>.28f && c.r>.28f && Mathf.Abs(c.g-c.r)<.10f && Mathf.Abs(c.b-c.g)<.10f) return Surface.Stone;
        return Surface.Cloth;
    }

    internal static Color Paint(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        return color;
    }

    internal static Color Tone(Color color, float value)
    {
        return new Color(color.r * value, color.g * value, color.b * value, 1f);
    }

    internal void Face(Vector3 a, Vector3 b, Vector3 c, Color color, Vector3 outside)
    {
        Face(a,b,c,color,color,color,outside);
    }

    internal void Face(Vector3 a,Vector3 b,Vector3 c,Color shadeA,Color shadeB,Color shadeC,Vector3 outside)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a);
        if (Vector3.Dot(normal, outside) < 0f)
        {
            Vector3 swap = b; b = c; c = swap;
            Color tint = shadeB; shadeB = shadeC; shadeC = tint;
            normal = -normal;
        }
        if (normal.sqrMagnitude < .0000000001f) return;
        normal.Normalize();
        int start = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c);
        normals.Add(normal); normals.Add(normal); normals.Add(normal);
        colors.Add(shadeA); colors.Add(shadeB); colors.Add(shadeC);
        Surface selected=surface==Surface.Auto?Pigment(shadeA):surface;
        Vector2 uv(Vector3 p)
        {
            Vector3 axis=new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z));
            Vector2 projected=axis.y>=axis.x && axis.y>=axis.z ? new Vector2(p.x,p.z)
                : axis.x>axis.z ? new Vector2(p.z,p.y) : new Vector2(p.x,p.y);
            return selected==Surface.Timber && grainRunsUp && axis.y<.65f
                ? new Vector2(projected.y,projected.x) : projected;
        }
        var channel=new Vector2((float)selected,0);
        uvs.Add(uv(a)); uvs.Add(uv(b)); uvs.Add(uv(c));
        paintLayers.Add(channel); paintLayers.Add(channel); paintLayers.Add(channel);
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
    }

    internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 outside)
    {
        Face(a, b, c, color, outside);
        Face(a, c, d, color, outside);
    }

    internal void Box(Vector3 position, Vector3 size, Color color, Quaternion rotation)
    {
        grainRunsUp=size.y>Mathf.Max(size.x,size.z)*1.45f;
        Vector3 p(float x, float y, float z) => position + rotation * Vector3.Scale(size, new Vector3(x, y, z)) * .5f;
        void side(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Quad(a, b, c, d, color, (a+b+c+d)*.25f-position);
        }
        side(p(-1,1,-1),p(-1,1,1),p(1,1,1),p(1,1,-1));
        side(p(-1,-1,-1),p(1,-1,-1),p(1,-1,1),p(-1,-1,1));
        side(p(-1,-1,-1),p(-1,1,-1),p(1,1,-1),p(1,-1,-1));
        side(p(1,-1,1),p(1,1,1),p(-1,1,1),p(-1,-1,1));
        side(p(-1,-1,1),p(-1,1,1),p(-1,1,-1),p(-1,-1,-1));
        side(p(1,-1,-1),p(1,1,-1),p(1,1,1),p(1,-1,1));
        grainRunsUp=false;
    }

    internal void Pole(Vector3 start, Vector3 end, float bottom, float top, Color color, int sides = 9)
    {
        Vector3 up = (end-start).normalized;
        grainRunsUp=Mathf.Abs(up.y)>.65f;
        Vector3 x = Vector3.Cross(up, Mathf.Abs(up.z) > .95f ? Vector3.right : Vector3.forward).normalized;
        Vector3 z = Vector3.Cross(up, x);
        for (int i=0; i<sides; i++)
        {
            Vector3 radial(int j) { float t=j*Mathf.PI*2/sides; return x*Mathf.Cos(t)+z*Mathf.Sin(t); }
            Vector3 r = radial(i), s = radial(i+1);
            Quad(start+r*bottom,start+s*bottom,end+s*top,end+r*top,color,r+s);
            Face(start,start+s*bottom,start+r*bottom,color,-up);
            Face(end,end+r*top,end+s*top,color,up);
        }
        grainRunsUp=false;
    }

    internal void Blob(Vector3 center, Vector3 radii, Color color, int seed, int sides = 8)
    {
        var random = new System.Random(seed);
        float noise() => (float)random.NextDouble();
        var rings = new Vector3[3,sides];
        for (int k=0;k<3;k++)
        {
            float y = (k-1)*.59f;
            for (int j=0;j<sides;j++)
            {
                float angle = (j+(k%2)*.39f+noise()*.13f)*Mathf.PI*2/sides;
                float width = Mathf.Sqrt(1-y*y)*(.91f+.14f*noise());
                Vector3 unit = new Vector3(Mathf.Cos(angle)*width,y+(noise()-.5f)*.13f,Mathf.Sin(angle)*width);
                rings[k,j] = center+Vector3.Scale(unit,radii);
            }
        }
        void facet(Vector3 a,Vector3 b,Vector3 c)
        {
            Vector3 n = (a+b+c)/3f-center;
            Face(a,b,c,Tone(color,.955f+.09f*noise()),n);
        }
        for(int j=0;j<sides;j++)
        {
            int next=(j+1)%sides;
            facet(center-Vector3.up*radii.y,rings[0,j],rings[0,next]);
            for(int k=0;k<2;k++)
            {
                facet(rings[k,j], rings[k,next],rings[k+1,j]);
                facet(rings[k,next],rings[k+1,next],rings[k+1,j]);
            }
            facet(rings[2,j],center+Vector3.up*radii.y,rings[2,next]);
        }
    }

    internal Mesh Finish(string name)
    {
        var mesh = new Mesh { name = name };
        if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetUVs(0,uvs);
        mesh.SetUVs(2,paintLayers);
        mesh.SetTriangles(triangles,0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
