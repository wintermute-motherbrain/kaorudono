using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LTreesLibrary.Trees
{
    /// <summary>
    /// Vertex used in leaf clouds.
    /// </summary>
    /// <remarks>
    /// A leaf consists of four vertices. Each vertex will have the same position, but different offsets.
    /// The vertex shader must use the offset to adjust the position of the output vertex to make the leaf face the camera.
    /// </remark>
    public struct LeafVertex
    {
        public struct IdStruct
        {
            public short Id;
            public short Unused;
        }

        /// <summary>
        /// Center of the leaf. 
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Texture coordinate.
        /// </summary>
        public Vector2 TextureCoordinate;

        /// <summary>
        /// Adjusts the vertex position in view space coordinates.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// Color tint of the leaf.
        /// </summary>
        public Vector4 Color;

        /// <summary>
        /// Index of the bone controlling this leaf.
        /// </summary>
        public IdStruct BoneIndex;

        /// <summary>
        /// Normal vector to use in lighting calculations.
        /// This is the orientation of the branch where the leaf was spawned.
        /// </summary>
        public Vector3 BranchNormal;

        public LeafVertex(Vector3 position, Vector2 textureCoordinate, Vector2 offset, Vector4 color, int bone, Vector3 normal)
        {
            this.Position = position;
            this.TextureCoordinate = textureCoordinate;
            this.Offset = offset;
            this.Color = color;
            this.BoneIndex.Id = (short) bone;
            this.BoneIndex.Unused = 0;
            this.BranchNormal = normal;
        }

        public static readonly VertexElement[] VertexElements = {
            new VertexElement(0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
            new VertexElement(0, 12, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(0, 20, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(0, 28, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Color, 0),
            new VertexElement(0, 44, VertexElementFormat.Short2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(0, 48, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
        };

        /// <summary>
        /// Number of bytes used by a vertex, which is 60 bytes.
        /// </summary>
        public const int SizeInBytes = sizeof(float) * (3 + 2 + 2 + 4 + 3) + sizeof(short) * 2;
    }
}
