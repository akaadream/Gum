﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework;
using System.IO;
using System.Collections.ObjectModel;
using RenderingLibrary.Math;
using RenderingLibrary;
using Microsoft.Xna.Framework.Content;

namespace RenderingLibrary.Graphics
{
    #region RenderStateVariables Class

    public class RenderStateVariables
    {
        public BlendState BlendState;
        public ColorOperation ColorOperation;
        public bool Filtering;
        public bool Wrap;

        public Rectangle? ClipRectangle;

        public RenderTarget2D RenderTarget;

    }

    #endregion

    public class Renderer
    {
        /// <summary>
        /// Whether renderable objects should call Render
        /// on contained children. This is true by default, 
        /// results in a hierarchical rendering order.
        /// </summary>
        public static bool RenderUsingHierarchy = true;

        #region Fields


        List<Layer> mLayers = new List<Layer>();
        ReadOnlyCollection<Layer> mLayersReadOnly;

        SpriteRenderer spriteRenderer = new SpriteRenderer();

        RenderStateVariables mRenderStateVariables = new RenderStateVariables();

        GraphicsDevice mGraphicsDevice;

        static Renderer mSelf;

        Camera mCamera;

        Texture2D mSinglePixelTexture;
        Texture2D mDottedLineTexture;

        public static object LockObject = new object();

        #endregion

        #region Properties

        internal float CurrentZoom
        {
            get
            {
                return spriteRenderer.CurrentZoom;
            }
            //private set;
        }

        public Layer MainLayer
        {
            get { return mLayers[0]; }
        }

        internal List<Layer> LayersWritable
        {
            get
            {
                return mLayers;
            }
        }

        public ReadOnlyCollection<Layer> Layers
        {
            get
            {
                return mLayersReadOnly;
            }
        }

        /// <summary>
        /// The texture used to render solid objects. If SinglePixelSourceRectangle is null, the entire texture is used. Otherwise
        /// the portion of SinglePixelTexture is applied.
        /// </summary>
        public Texture2D SinglePixelTexture
        {
            get
            {
#if DEBUG && !TEST
                // This should always be available
                if (mSinglePixelTexture == null)
                {
                    throw new InvalidOperationException("The single pixel texture is not set yet.  You must call Renderer.Initialize before accessing this property." +
                        "If running unit tests, be sure to run in UnitTest configuration");
                }
#endif
                return mSinglePixelTexture;
            }
            set
            {
                // Setter added to support rendering from sprite sheet.
                mSinglePixelTexture = value;
            }
        }

        /// <summary>
        /// The rectangle to use when rendering single-pixel texture objects, such as ColoredRectangles.
        /// By default this is null, indicating the entire texture is used.
        /// </summary>
        public Rectangle? SinglePixelSourceRectangle = null;

        public Texture2D DottedLineTexture
        {
            get
            {
#if DEBUG && !TEST
                // This should always be available
                if (mDottedLineTexture == null)
                {
                    throw new InvalidOperationException("The dotted line texture is not set yet.  You must call Renderer.Initialize before accessing this property." +
                        "If running unit tests, be sure to run in UnitTest configuration");
                }
#endif
                return mDottedLineTexture;
            }
        }

        public GraphicsDevice GraphicsDevice
        {
            get
            {
                return mGraphicsDevice;
            }
        }

        public static Renderer Self
        {
            get
            {
                // Why is this using a singleton instead of system managers default? This seems bad...

                //if (mSelf == null)
                //{
                //    mSelf = new Renderer();
                //}
                //return mSelf;
                if(SystemManagers.Default == null)
                {
                    throw new InvalidOperationException(
                        "The SystemManagers.Default is null. You must either specify the default SystemManagers, or use a custom SystemsManager if your app has multiple SystemManagers.");
                }
                return SystemManagers.Default.Renderer;

            }
        }

        public Camera Camera
        {
            get
            {
                return mCamera;
            }
        }

        public SamplerState SamplerState
        {
            get;
            set;
        }

        internal SpriteRenderer SpriteRenderer
        {
            get
            {
                return spriteRenderer;
            }
        }

        /// <summary>
        /// Controls which XNA BlendState is used for the Rendering Library's Blend.Normal value.
        /// </summary>
        /// <remarks>
        /// This should be either NonPremultiplied (if textures do not use premultiplied alpha), or
        /// AlphaBlend if using premultiplied alpha textures.
        /// </remarks>
        public static BlendState NormalBlendState
        {
            get;
            set;
        } = BlendState.NonPremultiplied;

        /// <summary>
        /// Use the custom effect for rendering. This setting takes priority if 
        /// both UseCustomEffectRendering and UseBasicEffectRendering are enabled.
        /// </summary>
        public static bool UseCustomEffectRendering { get; set; } = false;
        public static bool UseBasicEffectRendering { get; set; } = true;
        public static bool UsingEffect { get { return UseCustomEffectRendering || UseBasicEffectRendering; } }

        public static CustomEffectManager CustomEffectManager { get; } = new CustomEffectManager();

        /// <summary>
        /// When this is enabled texture colors will be translated to linear space before 
        /// any other shader operations are performed. This is useful for games with 
        /// lighting and other special shader effects. If the colors are left in gamma 
        /// space the shader calculations will crush the colors and not look like natural 
        /// lighting. Delinearization must be done by the developer in the last render 
        /// step when rendering to the screen. This technique is called gamma correction.
        /// Requires using the custom effect. Disabled by default.
        /// </summary>
        public static bool LinearizeTextures { get; set; }

        // Vic says March 29 2020
        // For some reason the rendering
        // in the tool works differently than
        // in-game. Not sure if this is a DesktopGL
        // vs XNA thing, but I traced it down to the zoom thing.
        // I'm going to add a bool here to control it.
        public static bool ApplyCameraZoomOnWorldTranslation { get; set; } = false;
        #endregion

        #region Methods

        public void Initialize(GraphicsDevice graphicsDevice, SystemManagers managers)
        {
            SamplerState = SamplerState.LinearClamp;
            mCamera = new RenderingLibrary.Camera(managers);
            mLayersReadOnly = new ReadOnlyCollection<Layer>(mLayers);

            mLayers.Add(new Layer());
            mLayers[0].Name = "Main Layer";

            mGraphicsDevice = graphicsDevice;

            spriteRenderer.Initialize(graphicsDevice);
            CustomEffectManager.Initialize(graphicsDevice);

            mSinglePixelTexture = new Texture2D(mGraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Color[] pixels = new Color[1];
            pixels[0] = Color.White;
            mSinglePixelTexture.SetData<Color>(pixels);
            mSinglePixelTexture.Name = "Rendering Library Single Pixel Texture";

            mDottedLineTexture = new Texture2D(mGraphicsDevice, 2, 1, false, SurfaceFormat.Color);
            mDottedLineTexture.Name = "Renderer Dotted Line Texture";
            pixels = new Color[2];
            pixels[0] = Color.White;
            pixels[1] = Color.Transparent;
            mDottedLineTexture.SetData<Color>(pixels);

            mCamera.UpdateClient();
        }

        public Layer AddLayer()
        {
            Layer layer = new Layer();
            mLayers.Add(layer);
            return layer;
        }


        //public void AddLayer(SortableLayer sortableLayer, Layer masterLayer)
        //{
        //    if (masterLayer == null)
        //    {
        //        masterLayer = LayersWritable[0];
        //    }

        //    masterLayer.Add(sortableLayer);
        //}

        public void Draw(SystemManagers managers)
        {
            ClearPerformanceRecordingVariables();

            if (managers == null)
            {
                managers = SystemManagers.Default;
            }

            Draw(managers, mLayers);

            ForceEnd();
        }

        public void Draw(SystemManagers managers, Layer layer)
        {
            // So that 2 controls don't render at the same time.
            lock (LockObject)
            {
                mCamera.UpdateClient();

                var oldSampler = GraphicsDevice.SamplerStates[0];

                mRenderStateVariables.BlendState = Renderer.NormalBlendState;
                mRenderStateVariables.Wrap = false;

                RenderLayer(managers, layer);

                if (oldSampler != null)
                {
                    GraphicsDevice.SamplerStates[0] = oldSampler;
                }
            }
        }

        public void Draw(SystemManagers managers, List<Layer> layers)
        {
            // So that 2 controls don't render at the same time.
            lock (LockObject)
            {
                mCamera.UpdateClient();


                mRenderStateVariables.BlendState = Renderer.NormalBlendState;
                mRenderStateVariables.Wrap = false;

                for (int i = 0; i < layers.Count; i++)
                {
                    PreRender(layers[i].Renderables);
                }

                for (int i = 0; i < layers.Count; i++)
                {
                    Layer layer = layers[i];
                    RenderLayer(managers, layer, prerender:false);
                }
            }
        }

        internal void RenderLayer(SystemManagers managers, Layer layer, bool prerender = true)
        {
            //////////////////Early Out////////////////////////////////
            if (layer.Renderables.Count == 0)
            {
                return;
            }
            ///////////////End Early Out///////////////////////////////

            if (prerender)
            {
                PreRender(layer.Renderables);
            }

            SpriteBatchStack.PerformStartOfLayerRenderingLogic();

            spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Push, mCamera);

            layer.SortRenderables();

            Render(layer.Renderables, managers, layer);

            spriteRenderer.EndSpriteBatch();
        }

        private void PreRender(IList<IRenderableIpso> renderables)
        {
#if DEBUG
            if(renderables == null)
            {
                throw new ArgumentNullException("renderables");
            }
#endif

            var count = renderables.Count;
            for(int i = 0; i < count; i++)
            {
                var renderable = renderables[i];
                if(renderable.Visible)
                {
                    renderable.PreRender();

                    // Some Gum objects, like GraphicalUiElements, may not have children if the object hasn't
                    // yet been assigned a visual. Just skip over it...
                    if(renderable.Visible && renderable.Children != null)
                    {
                        PreRender(renderable.Children);
                    }
                }
            }
        }

        private void Render(IList<IRenderableIpso> whatToRender, SystemManagers managers, Layer layer)
        {
            var count = whatToRender.Count;
            for (int i = 0; i < count; i++)
            {
                var renderable = whatToRender[i];
                if(renderable.Visible)
                {
                    var oldClip = mRenderStateVariables.ClipRectangle;
                    var oldRenderTarget = mRenderStateVariables.RenderTarget;

                    AdjustRenderStateVariables(mRenderStateVariables, layer, renderable);

                    bool didClipChange = oldClip != mRenderStateVariables.ClipRectangle;
                    bool didRenderTargetChange = oldRenderTarget != mRenderStateVariables.RenderTarget;

                    renderable.Render(spriteRenderer, managers);


                    if (RenderUsingHierarchy && renderable.Children.Count > 0)
                    {
                        Render(renderable.Children, managers, layer);
                    }

                    if(didRenderTargetChange)
                    {
                        var toDispose = mRenderStateVariables.RenderTarget;
                        
                        mRenderStateVariables.RenderTarget = oldRenderTarget;

                        spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Begin, mCamera);
                        spriteRenderer.Draw(toDispose, new Rectangle(0, 0, toDispose.Width, toDispose.Height), new Rectangle(0, 0, toDispose.Width, toDispose.Height), Color.White, null);

                        // apply the drawn render target:
                        spriteRenderer.End();


                        toDispose?.Dispose() ;
                    }

                    if (didClipChange)
                    {
                        mRenderStateVariables.ClipRectangle = oldClip;

                        spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Begin, mCamera);
                    }
                }
            }
        }

        internal Microsoft.Xna.Framework.Rectangle GetScissorRectangleFor(Camera camera, IRenderableIpso ipso)
        {
            if (ipso == null)
            {
                return new Microsoft.Xna.Framework.Rectangle(
                    0, 0,
                    camera.ClientWidth,
                    camera.ClientHeight

                    );
            }
            else
            {

                float worldX = ipso.GetAbsoluteLeft();
                float worldY = ipso.GetAbsoluteTop();

                float screenX;
                float screenY;
                camera.WorldToScreen(worldX, worldY, out screenX, out screenY);

                int left = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenX);
                int top = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenY);

                worldX = ipso.GetAbsoluteRight();
                worldY = ipso.GetAbsoluteBottom();
                camera.WorldToScreen(worldX, worldY, out screenX, out screenY);

                int right = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenX);
                int bottom = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenY);



                left = System.Math.Max(0, left);
                top = System.Math.Max(0, top);
                right = System.Math.Max(0, right);
                bottom = System.Math.Max(0, bottom);

                left = System.Math.Min(left, camera.ClientWidth);
                right = System.Math.Min(right, camera.ClientWidth);

                top = System.Math.Min(top, camera.ClientHeight);
                bottom = System.Math.Min(bottom, camera.ClientHeight);


                int width = System.Math.Max(0, right - left);
                int height = System.Math.Max(0, bottom - top);

                // ScissorRectangles are relative to the viewport in Gum, so we need to adjust for that:
                left += this.GraphicsDevice.Viewport.X;
                right += this.GraphicsDevice.Viewport.X;

                top += this.GraphicsDevice.Viewport.Y;
                bottom += this.GraphicsDevice.Viewport.Y;

                Microsoft.Xna.Framework.Rectangle thisRectangle = new Microsoft.Xna.Framework.Rectangle(
                    left,
                    top,
                    width,
                    height);

                return thisRectangle;
            }

        }


        private void AdjustRenderStateVariables(RenderStateVariables renderState, Layer layer, IRenderableIpso renderable)
        {
            BlendState renderBlendState = renderable.BlendState;
            bool wrap = renderable.Wrap;
            bool shouldResetStates = false;

            if (renderBlendState == null)
            {
                renderBlendState = Renderer.NormalBlendState;
            }
            if (renderState.BlendState != renderBlendState)
            {
                // This used to set this, but not sure why...I think it should set the renderBlendState:
                //renderState.BlendState = renderable.BlendState;
                renderState.BlendState = renderBlendState;

                shouldResetStates = true;

            }

            if(renderState.ColorOperation != renderable.ColorOperation)
            {
                renderState.ColorOperation = renderable.ColorOperation;
                shouldResetStates = true;
            }

            if (renderState.Wrap != wrap)
            {
                renderState.Wrap = wrap;
                shouldResetStates = true;
            }

            if (renderable.ClipsChildren)
            {

                var userRenderTarget = true;
                if(userRenderTarget)
                {
                    var renderTarget = new RenderTarget2D(GraphicsDevice, MathFunctions.RoundToInt(renderable.Width), MathFunctions.RoundToInt(renderable.Height));
                    renderState.RenderTarget = renderTarget;
                    shouldResetStates = true;
                }
                else
                {
                    Rectangle clipRectangle = GetScissorRectangleFor(Camera, renderable);

                    if (renderState.ClipRectangle == null || clipRectangle != renderState.ClipRectangle.Value)
                    {
                        //todo: Don't just overwrite it, constrain this rect to the existing one, if it's not null: 

                        var adjustedRectangle = clipRectangle;
                        if (renderState.ClipRectangle != null)
                        {
                            adjustedRectangle = ConstrainRectangle(clipRectangle, renderState.ClipRectangle.Value);
                        }


                        renderState.ClipRectangle = adjustedRectangle;
                        shouldResetStates = true;
                    }
                }

            }


            if (shouldResetStates)
            {
                // this ends the old sprite batch, so we want to end it first...
                spriteRenderer.BeginSpriteBatch(renderState, layer, BeginType.Begin, mCamera);
            }
        }

        private Rectangle ConstrainRectangle(Rectangle childRectangle, Rectangle parentRectangle)
        {
            int x = System.Math.Max(childRectangle.X, parentRectangle.X);
            int y = System.Math.Max(childRectangle.Y, parentRectangle.Y);

            int right = System.Math.Min(childRectangle.Right, parentRectangle.Right);
            int bottom = System.Math.Min(childRectangle.Bottom, parentRectangle.Bottom);

            return new Rectangle(x, y, right - x, bottom - y);
        }

        // Made public to allow custom renderable objects to be removed:
        public void RemoveRenderable(IRenderableIpso renderable)
        {
            foreach (Layer layer in this.Layers)
            {
                if (layer.Renderables.Contains(renderable))
                {
                    layer.Remove(renderable);
                }
            }
        }

        //public void RemoveLayer(SortableLayer sortableLayer)
        //{
        //    RemoveRenderable(sortableLayer);
        //}

        public void RemoveLayer(Layer layer)
        {
            mLayers.Remove(layer);
        }

        public void ClearPerformanceRecordingVariables()
        {
            spriteRenderer.ClearPerformanceRecordingVariables();
        }

        /// <summary>
        /// Ends the current SpriteBatchif it hasn't yet been ended. This is needed for projects which may need the
        /// rendering to end itself so that they can start sprite batch.
        /// </summary>
        public void ForceEnd()
        {
            this.spriteRenderer.End();

        }

        #endregion


    }

    #region Custom effect support

    public class CustomEffectManager
    {
        Effect mEffect;

        // Cached effect members to avoid list lookups while rendering
        public EffectParameter ParameterCurrentTexture;
        public EffectParameter ParameterViewProj;
        public EffectParameter ParameterColorModifier;

        bool mEffectHasNewformat;

        EffectTechnique mTechniqueTexture;
        EffectTechnique mTechniqueAdd;
        EffectTechnique mTechniqueSubtract;
        EffectTechnique mTechniqueModulate;
        EffectTechnique mTechniqueModulate2X;
        EffectTechnique mTechniqueModulate4X;
        EffectTechnique mTechniqueInverseTexture;
        EffectTechnique mTechniqueColor;
        EffectTechnique mTechniqueColorTextureAlpha;
        EffectTechnique mTechniqueInterpolateColor;

        EffectTechnique mTechniqueTexture_CM;
        EffectTechnique mTechniqueAdd_CM;
        EffectTechnique mTechniqueSubtract_CM;
        EffectTechnique mTechniqueModulate_CM;
        EffectTechnique mTechniqueModulate2X_CM;
        EffectTechnique mTechniqueModulate4X_CM;
        EffectTechnique mTechniqueInverseTexture_CM;
        EffectTechnique mTechniqueColor_CM;
        EffectTechnique mTechniqueColorTextureAlpha_CM;
        EffectTechnique mTechniqueInterpolateColor_CM;

        EffectTechnique mTechniqueTexture_LN;
        EffectTechnique mTechniqueAdd_LN;
        EffectTechnique mTechniqueSubtract_LN;
        EffectTechnique mTechniqueModulate_LN;
        EffectTechnique mTechniqueModulate2X_LN;
        EffectTechnique mTechniqueModulate4X_LN;
        EffectTechnique mTechniqueInverseTexture_LN;
        EffectTechnique mTechniqueColor_LN;
        EffectTechnique mTechniqueColorTextureAlpha_LN;
        EffectTechnique mTechniqueInterpolateColor_LN;

        EffectTechnique mTechniqueTexture_LN_CM;
        EffectTechnique mTechniqueAdd_LN_CM;
        EffectTechnique mTechniqueSubtract_LN_CM;
        EffectTechnique mTechniqueModulate_LN_CM;
        EffectTechnique mTechniqueModulate2X_LN_CM;
        EffectTechnique mTechniqueModulate4X_LN_CM;
        EffectTechnique mTechniqueInverseTexture_LN_CM;
        EffectTechnique mTechniqueColor_LN_CM;
        EffectTechnique mTechniqueColorTextureAlpha_LN_CM;
        EffectTechnique mTechniqueInterpolateColor_LN_CM;

        EffectTechnique mTechniqueTexture_Linear;
        EffectTechnique mTechniqueAdd_Linear;
        EffectTechnique mTechniqueSubtract_Linear;
        EffectTechnique mTechniqueModulate_Linear;
        EffectTechnique mTechniqueModulate2X_Linear;
        EffectTechnique mTechniqueModulate4X_Linear;
        EffectTechnique mTechniqueInverseTexture_Linear;
        EffectTechnique mTechniqueColor_Linear;
        EffectTechnique mTechniqueColorTextureAlpha_Linear;
        EffectTechnique mTechniqueInterpolateColor_Linear;

        EffectTechnique mTechniqueTexture_Linear_CM;
        EffectTechnique mTechniqueAdd_Linear_CM;
        EffectTechnique mTechniqueSubtract_Linear_CM;
        EffectTechnique mTechniqueModulate_Linear_CM;
        EffectTechnique mTechniqueModulate2X_Linear_CM;
        EffectTechnique mTechniqueModulate4X_Linear_CM;
        EffectTechnique mTechniqueInverseTexture_Linear_CM;
        EffectTechnique mTechniqueColor_Linear_CM;
        EffectTechnique mTechniqueColorTextureAlpha_Linear_CM;
        EffectTechnique mTechniqueInterpolateColor_Linear_CM;

        EffectTechnique mTechniqueTexture_Linear_LN;
        EffectTechnique mTechniqueAdd_Linear_LN;
        EffectTechnique mTechniqueSubtract_Linear_LN;
        EffectTechnique mTechniqueModulate_Linear_LN;
        EffectTechnique mTechniqueModulate2X_Linear_LN;
        EffectTechnique mTechniqueModulate4X_Linear_LN;
        EffectTechnique mTechniqueInverseTexture_Linear_LN;
        EffectTechnique mTechniqueColor_Linear_LN;
        EffectTechnique mTechniqueColorTextureAlpha_Linear_LN;
        EffectTechnique mTechniqueInterpolateColor_Linear_LN;

        EffectTechnique mTechniqueTexture_Linear_LN_CM;
        EffectTechnique mTechniqueAdd_Linear_LN_CM;
        EffectTechnique mTechniqueSubtract_Linear_LN_CM;
        EffectTechnique mTechniqueModulate_Linear_LN_CM;
        EffectTechnique mTechniqueModulate2X_Linear_LN_CM;
        EffectTechnique mTechniqueModulate4X_Linear_LN_CM;
        EffectTechnique mTechniqueInverseTexture_Linear_LN_CM;
        EffectTechnique mTechniqueColor_Linear_LN_CM;
        EffectTechnique mTechniqueColorTextureAlpha_Linear_LN_CM;
        EffectTechnique mTechniqueInterpolateColor_Linear_LN_CM;

        public Effect Effect
        {
            get { return mEffect; }
            private set
            {
                mEffect = value;

                ParameterViewProj = mEffect.Parameters["ViewProj"];
                ParameterCurrentTexture = mEffect.Parameters["CurrentTexture"];
                try { ParameterColorModifier = mEffect.Parameters["ColorModifier"]; } catch { }

                // Let's check if the shader has the new format (which includes
                // separate versions of techniques for Point and Linear filtering).
                // We try to cache the first technique in order to do so.
                try { mTechniqueTexture = mEffect.Techniques["Texture_Point"]; } catch { }

                if (mTechniqueTexture != null)
                {
                    mEffectHasNewformat = true;

                    //try { mTechniqueTexture = mEffect.Techniques["Texture_Point"]; } catch { } // This has been already cached
                    try { mTechniqueAdd = mEffect.Techniques["Add_Point"]; } catch { }
                    try { mTechniqueSubtract = mEffect.Techniques["Subtract_Point"]; } catch { }
                    try { mTechniqueModulate = mEffect.Techniques["Modulate_Point"]; } catch { }
                    try { mTechniqueModulate2X = mEffect.Techniques["Modulate2X_Point"]; } catch { }
                    try { mTechniqueModulate4X = mEffect.Techniques["Modulate4X_Point"]; } catch { }
                    try { mTechniqueInverseTexture = mEffect.Techniques["InverseTexture_Point"]; } catch { }
                    try { mTechniqueColor = mEffect.Techniques["Color_Point"]; } catch { }
                    try { mTechniqueColorTextureAlpha = mEffect.Techniques["ColorTextureAlpha_Point"]; } catch { }
                    try { mTechniqueInterpolateColor = mEffect.Techniques["InterpolateColor_Point"]; } catch { }

                    try { mTechniqueTexture_CM = mEffect.Techniques["Texture_Point_CM"]; } catch { }
                    try { mTechniqueAdd_CM = mEffect.Techniques["Add_Point_CM"]; } catch { }
                    try { mTechniqueSubtract_CM = mEffect.Techniques["Subtract_Point_CM"]; } catch { }
                    try { mTechniqueModulate_CM = mEffect.Techniques["Modulate_Point_CM"]; } catch { }
                    try { mTechniqueModulate2X_CM = mEffect.Techniques["Modulate2X_Point_CM"]; } catch { }
                    try { mTechniqueModulate4X_CM = mEffect.Techniques["Modulate4X_Point_CM"]; } catch { }
                    try { mTechniqueInverseTexture_CM = mEffect.Techniques["InverseTexture_Point_CM"]; } catch { }
                    try { mTechniqueColor_CM = mEffect.Techniques["Color_Point_CM"]; } catch { }
                    try { mTechniqueColorTextureAlpha_CM = mEffect.Techniques["ColorTextureAlpha_Point_CM"]; } catch { }
                    try { mTechniqueInterpolateColor_CM = mEffect.Techniques["InterpolateColor_Point_CM"]; } catch { }

                    try { mTechniqueTexture_LN = mEffect.Techniques["Texture_Point_LN"]; } catch { }
                    try { mTechniqueAdd_LN = mEffect.Techniques["Add_Point_LN"]; } catch { }
                    try { mTechniqueSubtract_LN = mEffect.Techniques["Subtract_Point_LN"]; } catch { }
                    try { mTechniqueModulate_LN = mEffect.Techniques["Modulate_Point_LN"]; } catch { }
                    try { mTechniqueModulate2X_LN = mEffect.Techniques["Modulate2X_Point_LN"]; } catch { }
                    try { mTechniqueModulate4X_LN = mEffect.Techniques["Modulate4X_Point_LN"]; } catch { }
                    try { mTechniqueInverseTexture_LN = mEffect.Techniques["InverseTexture_Point_LN"]; } catch { }
                    try { mTechniqueColor_LN = mEffect.Techniques["Color_Point_LN"]; } catch { }
                    try { mTechniqueColorTextureAlpha_LN = mEffect.Techniques["ColorTextureAlpha_Point_LN"]; } catch { }
                    try { mTechniqueInterpolateColor_LN = mEffect.Techniques["InterpolateColor_Point_LN"]; } catch { }

                    try { mTechniqueTexture_LN_CM = mEffect.Techniques["Texture_Point_LN_CM"]; } catch { }
                    try { mTechniqueAdd_LN_CM = mEffect.Techniques["Add_Point_LN_CM"]; } catch { }
                    try { mTechniqueSubtract_LN_CM = mEffect.Techniques["Subtract_Point_LN_CM"]; } catch { }
                    try { mTechniqueModulate_LN_CM = mEffect.Techniques["Modulate_Point_LN_CM"]; } catch { }
                    try { mTechniqueModulate2X_LN_CM = mEffect.Techniques["Modulate2X_Point_LN_CM"]; } catch { }
                    try { mTechniqueModulate4X_LN_CM = mEffect.Techniques["Modulate4X_Point_LN_CM"]; } catch { }
                    try { mTechniqueInverseTexture_LN_CM = mEffect.Techniques["InverseTexture_Point_LN_CM"]; } catch { }
                    try { mTechniqueColor_LN_CM = mEffect.Techniques["Color_Point_LN_CM"]; } catch { }
                    try { mTechniqueColorTextureAlpha_LN_CM = mEffect.Techniques["ColorTextureAlpha_Point_LN_CM"]; } catch { }
                    try { mTechniqueInterpolateColor_LN_CM = mEffect.Techniques["InterpolateColor_Point_LN_CM"]; } catch { }

                    try { mTechniqueTexture_Linear = mEffect.Techniques["Texture_Linear"]; } catch { }
                    try { mTechniqueAdd_Linear = mEffect.Techniques["Add_Linear"]; } catch { }
                    try { mTechniqueSubtract_Linear = mEffect.Techniques["Subtract_Linear"]; } catch { }
                    try { mTechniqueModulate_Linear = mEffect.Techniques["Modulate_Linear"]; } catch { }
                    try { mTechniqueModulate2X_Linear = mEffect.Techniques["Modulate2X_Linear"]; } catch { }
                    try { mTechniqueModulate4X_Linear = mEffect.Techniques["Modulate4X_Linear"]; } catch { }
                    try { mTechniqueInverseTexture_Linear = mEffect.Techniques["InverseTexture_Linear"]; } catch { }
                    try { mTechniqueColor_Linear = mEffect.Techniques["Color_Linear"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linear = mEffect.Techniques["ColorTextureAlpha_Linear"]; } catch { }
                    try { mTechniqueInterpolateColor_Linear = mEffect.Techniques["InterpolateColor_Linear"]; } catch { }

                    try { mTechniqueTexture_Linear_CM = mEffect.Techniques["Texture_Linear_CM"]; } catch { }
                    try { mTechniqueAdd_Linear_CM = mEffect.Techniques["Add_Linear_CM"]; } catch { }
                    try { mTechniqueSubtract_Linear_CM = mEffect.Techniques["Subtract_Linear_CM"]; } catch { }
                    try { mTechniqueModulate_Linear_CM = mEffect.Techniques["Modulate_Linear_CM"]; } catch { }
                    try { mTechniqueModulate2X_Linear_CM = mEffect.Techniques["Modulate2X_Linear_CM"]; } catch { }
                    try { mTechniqueModulate4X_Linear_CM = mEffect.Techniques["Modulate4X_Linear_CM"]; } catch { }
                    try { mTechniqueInverseTexture_Linear_CM = mEffect.Techniques["InverseTexture_Linear_CM"]; } catch { }
                    try { mTechniqueColor_Linear_CM = mEffect.Techniques["Color_Linear_CM"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linear_CM = mEffect.Techniques["ColorTextureAlpha_Linear_CM"]; } catch { }
                    try { mTechniqueInterpolateColor_Linear_CM = mEffect.Techniques["InterpolateColor_Linear_CM"]; } catch { }

                    try { mTechniqueTexture_Linear_LN = mEffect.Techniques["Texture_Linear_LN"]; } catch { }
                    try { mTechniqueAdd_Linear_LN = mEffect.Techniques["Add_Linear_LN"]; } catch { }
                    try { mTechniqueSubtract_Linear_LN = mEffect.Techniques["Subtract_Linear_LN"]; } catch { }
                    try { mTechniqueModulate_Linear_LN = mEffect.Techniques["Modulate_Linear_LN"]; } catch { }
                    try { mTechniqueModulate2X_Linear_LN = mEffect.Techniques["Modulate2X_Linear_LN"]; } catch { }
                    try { mTechniqueModulate4X_Linear_LN = mEffect.Techniques["Modulate4X_Linear_LN"]; } catch { }
                    try { mTechniqueInverseTexture_Linear_LN = mEffect.Techniques["InverseTexture_Linear_LN"]; } catch { }
                    try { mTechniqueColor_Linear_LN = mEffect.Techniques["Color_Linear_LN"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linear_LN = mEffect.Techniques["ColorTextureAlpha_Linear_LN"]; } catch { }
                    try { mTechniqueInterpolateColor_Linear_LN = mEffect.Techniques["InterpolateColor_Linear_LN"]; } catch { }

                    try { mTechniqueTexture_Linear_LN_CM = mEffect.Techniques["Texture_Linear_LN_CM"]; } catch { }
                    try { mTechniqueAdd_Linear_LN_CM = mEffect.Techniques["Add_Linear_LN_CM"]; } catch { }
                    try { mTechniqueSubtract_Linear_LN_CM = mEffect.Techniques["Subtract_Linear_LN_CM"]; } catch { }
                    try { mTechniqueModulate_Linear_LN_CM = mEffect.Techniques["Modulate_Linear_LN_CM"]; } catch { }
                    try { mTechniqueModulate2X_Linear_LN_CM = mEffect.Techniques["Modulate2X_Linear_LN_CM"]; } catch { }
                    try { mTechniqueModulate4X_Linear_LN_CM = mEffect.Techniques["Modulate4X_Linear_LN_CM"]; } catch { }
                    try { mTechniqueInverseTexture_Linear_LN_CM = mEffect.Techniques["InverseTexture_Linear_LN_CM"]; } catch { }
                    try { mTechniqueColor_Linear_LN_CM = mEffect.Techniques["Color_Linear_LN_CM"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linear_LN_CM = mEffect.Techniques["ColorTextureAlpha_Linear_LN_CM"]; } catch { }
                    try { mTechniqueInterpolateColor_Linear_LN_CM = mEffect.Techniques["InterpolateColor_Linear_LN_CM"]; } catch { }
                }
                else
                {
                    mEffectHasNewformat = false;

                    try { mTechniqueTexture = mEffect.Techniques["Texture"]; } catch { }
                    try { mTechniqueAdd = mEffect.Techniques["Add"]; } catch { }
                    try { mTechniqueSubtract = mEffect.Techniques["Subtract"]; } catch { }
                    try { mTechniqueModulate = mEffect.Techniques["Modulate"]; } catch { }
                    try { mTechniqueModulate2X = mEffect.Techniques["Modulate2X"]; } catch { }
                    try { mTechniqueModulate4X = mEffect.Techniques["Modulate4X"]; } catch { }
                    try { mTechniqueInverseTexture = mEffect.Techniques["InverseTexture"]; } catch { }
                    try { mTechniqueColor = mEffect.Techniques["Color"]; } catch { }
                    try { mTechniqueColorTextureAlpha = mEffect.Techniques["ColorTextureAlpha"]; } catch { }
                    try { mTechniqueInterpolateColor = mEffect.Techniques["InterpolateColor"]; } catch { }
                }
            }
        }

        public class ServiceContainer : IServiceProvider
        {
            #region Fields

            Dictionary<Type, object> services = new Dictionary<Type, object>();

            #endregion

            #region Methods

            public void AddService<T>(T service)
            {
                services.Add(typeof(T), service);
            }

            public object GetService(Type serviceType)
            {
                object service;

                services.TryGetValue(serviceType, out service);

                return service;
            }

            #endregion
        }

        static ContentManager mContentManager;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            if (mContentManager == null)
            {
                mContentManager = new ContentManager(
                                  new ServiceProvider(
                                       new DeviceManager(graphicsDevice)));
            }

            try { Effect = mContentManager.Load<Effect>("Content/shader"); } catch { }
        }

        static EffectTechnique GetTechniqueVariant(bool useDefaultOrPointFilter, EffectTechnique point, EffectTechnique pointLinearized, EffectTechnique linear, EffectTechnique linearLinearized)
        {
            return useDefaultOrPointFilter ?
                (Renderer.LinearizeTextures ? pointLinearized : point) :
                (Renderer.LinearizeTextures ? linearLinearized : linear);
        }

        public EffectTechnique GetVertexColorTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
        {
            if (mEffect == null)
                throw new Exception("The effect hasn't been set.");

            EffectTechnique technique = null;

            bool useDefaultOrPointFilterInternal;

            if (mEffectHasNewformat)
            {
                // If the shader has the new format both point and linear are available
                if (!useDefaultOrPointFilter.HasValue)
                {
                    // Filter not specified, we don't seem to have general setting for
                    // filtering in Gum so we'll use the default.
                    useDefaultOrPointFilterInternal = true;
                }
                else
                {
                    // Filter specified
                    useDefaultOrPointFilterInternal = useDefaultOrPointFilter.Value;
                }
            }
            else
            {
                // If the shader doesn't have the new format only one version of
                // the techniques are available, probably using point filtering.
                useDefaultOrPointFilterInternal = true;
            }

            // Only Modulate and ColorTextureAlpha are available in Gum at the moment
            switch (value)
            {
                //case ColorOperation.Texture:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueTexture, mTechniqueTexture_LN, mTechniqueTexture_Linear, mTechniqueTexture_Linear_LN); break;

                //case ColorOperation.Add:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueAdd, mTechniqueAdd_LN, mTechniqueAdd_Linear, mTechniqueAdd_Linear_LN); break;

                //case ColorOperation.Subtract:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueSubtract, mTechniqueSubtract_LN, mTechniqueSubtract_Linear, mTechniqueSubtract_Linear_LN); break;

                case ColorOperation.Modulate:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueModulate, mTechniqueModulate_LN, mTechniqueModulate_Linear, mTechniqueModulate_Linear_LN); break;

                //case ColorOperation.Modulate2X:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueModulate2X, mTechniqueModulate2X_LN, mTechniqueModulate2X_Linear, mTechniqueModulate2X_Linear_LN); break;

                //case ColorOperation.Modulate4X:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueModulate4X, mTechniqueModulate4X_LN, mTechniqueModulate4X_Linear, mTechniqueModulate4X_Linear_LN); break;

                //case ColorOperation.InverseTexture:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueInverseTexture, mTechniqueInverseTexture_LN, mTechniqueInverseTexture_Linear, mTechniqueInverseTexture_Linear_LN); break;

                //case ColorOperation.Color:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueColor, mTechniqueColor_LN, mTechniqueColor_Linear, mTechniqueColor_Linear_LN); break;

                case ColorOperation.ColorTextureAlpha:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueColorTextureAlpha, mTechniqueColorTextureAlpha_LN, mTechniqueColorTextureAlpha_Linear, mTechniqueColorTextureAlpha_Linear_LN); break;

                //case ColorOperation.InterpolateColor:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueInterpolateColor, mTechniqueInterpolateColor_LN, mTechniqueInterpolateColor_Linear, mTechniqueInterpolateColor_Linear_LN); break;

                default: throw new InvalidOperationException();
            }

            return technique;
        }

        public EffectTechnique GetColorModifierTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
        {
            if (mEffect == null)
                throw new Exception("The effect hasn't been set.");

            EffectTechnique technique = null;

            bool useDefaultOrPointFilterInternal;

            if (mEffectHasNewformat)
            {
                // If the shader has the new format both point and linear are available
                if (!useDefaultOrPointFilter.HasValue)
                {
                    // Filter not specified, we don't seem to have general setting for
                    // filtering in Gum so we'll use the default.
                    useDefaultOrPointFilterInternal = true;
                }
                else
                {
                    // Filter specified
                    useDefaultOrPointFilterInternal = useDefaultOrPointFilter.Value;
                }
            }
            else
            {
                // If the shader doesn't have the new format only one version of
                // the techniques are available, probably using point filtering.
                useDefaultOrPointFilterInternal = true;
            }

            switch (value)
            {
                //case ColorOperation.Texture:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueTexture_CM, mTechniqueTexture_LN_CM, mTechniqueTexture_Linear_CM, mTechniqueTexture_Linear_LN_CM); break;

                //case ColorOperation.Add:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueAdd_CM, mTechniqueAdd_LN_CM, mTechniqueAdd_Linear_CM, mTechniqueAdd_Linear_LN_CM); break;

                //case ColorOperation.Subtract:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueSubtract_CM, mTechniqueSubtract_LN_CM, mTechniqueSubtract_Linear_CM, mTechniqueSubtract_Linear_LN_CM); break;

                case ColorOperation.Modulate:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueModulate_CM, mTechniqueModulate_LN_CM, mTechniqueModulate_Linear_CM, mTechniqueModulate_Linear_LN_CM); break;

                //case ColorOperation.Modulate2X:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueModulate2X_CM, mTechniqueModulate2X_LN_CM, mTechniqueModulate2X_Linear_CM, mTechniqueModulate2X_Linear_LN_CM); break;

                //case ColorOperation.Modulate4X:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueModulate4X_CM, mTechniqueModulate4X_LN_CM, mTechniqueModulate4X_Linear_CM, mTechniqueModulate4X_Linear_LN_CM); break;

                //case ColorOperation.InverseTexture:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueInverseTexture_CM, mTechniqueInverseTexture_LN_CM, mTechniqueInverseTexture_Linear_CM, mTechniqueInverseTexture_Linear_LN_CM); break;

                //case ColorOperation.Color:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueColor_CM, mTechniqueColor_LN_CM, mTechniqueColor_Linear_CM, mTechniqueColor_Linear_LN_CM); break;

                case ColorOperation.ColorTextureAlpha:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueColorTextureAlpha_CM, mTechniqueColorTextureAlpha_LN_CM, mTechniqueColorTextureAlpha_Linear_CM, mTechniqueColorTextureAlpha_Linear_LN_CM); break;

                //case ColorOperation.InterpolateColor:
                //    technique = GetTechniqueVariant(
                //    useDefaultOrPointFilterInternal, mTechniqueInterpolateColor_CM, mTechniqueInterpolateColor_LN_CM, mTechniqueInterpolateColor_Linear_CM, mTechniqueInterpolateColor_Linear_LN_CM); break;

                default: throw new InvalidOperationException();
            }

            return technique;
        }

        //public static Vector4 ProcessColorForColorOperation(ColorOperation colorOperation, Vector4 input)
        //{
        //    if (colorOperation == ColorOperation.Color)
        //    {
        //        return new Vector4(input.X * input.W, input.Y * input.W, input.Z * input.W, input.W);
        //    }
        //    else if (colorOperation == ColorOperation.Texture)
        //    {
        //        return new Vector4(input.W, input.W, input.W, input.W);
        //    }
        //    else
        //    {
        //        return new Vector4(input.X, input.Y, input.Z, input.W);
        //    }
        //}
    }

    public class DeviceManager : IGraphicsDeviceService
    {
        public DeviceManager(GraphicsDevice device)
        {
            GraphicsDevice = device;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;
    }

    public class ServiceProvider : IServiceProvider
    {
        private readonly IGraphicsDeviceService deviceService;

        public ServiceProvider(IGraphicsDeviceService deviceService)
        {
            this.deviceService = deviceService;
        }

        public object GetService(Type serviceType)
        {
            return deviceService;
        }
    }

    #endregion
}
