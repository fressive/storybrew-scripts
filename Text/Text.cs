using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using StorybrewCommon.Storyboarding.Util;
using StorybrewCommon.Subtitles;
using StorybrewCommon.Util;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using SharpYaml.Serialization;

namespace StorybrewScripts
{
    public class Text : StoryboardObjectGenerator
    {
        [Configurable]
        public string Path;

        [Configurable]
        public string LayerName;

        [Configurable]
        public string SpritesPath = "sb/f";

        [Configurable]
        public string Font = "Verdana";

        [Configurable]
        public int FontSize = 26;

        [Configurable]
        public float FontScale = 0.5f;

        [Configurable]
        public FontStyle FontStyle = FontStyle.Regular;

        [Configurable]
        public Color4 FontColor = new Color4(255, 255, 255, 200);

        [Configurable]
        public int GlowRadius = 0;

        [Configurable]
        public Color4 GlowColor = new Color4(255, 255, 255, 100);

        [Configurable]
        public bool AdditiveGlow = true;

        [Configurable]
        public int OutlineThickness = 3;

        [Configurable]
        public Color4 OutlineColor = new Color4(50, 50, 50, 200);

        [Configurable]
        public int ShadowThickness = 0;

        [Configurable]
        public Color4 ShadowColor = new Color4(0, 0, 0, 100);

        [Configurable]
        public OsbOrigin Origin = OsbOrigin.Centre;

        public class TextConfiguration : ICloneable {
            [YamlMember("color")]
            public string Color = null;

            [YamlMember("padding")]
            public float?[] Padding = null;

            [YamlMember("paddingLine")]
            public float?[] PaddingLine = null;

            [YamlMember("paddingSpace")]
            public float?[] PaddingSpace = null;
            
            [YamlMember("position")]
            public float?[] Position = null;

            [YamlMember("positionOffset")]
            public float?[] PositionOffset = null;

            [YamlMember("orientation")]
            public int? Orientation = null;

            [YamlMember("fadeIn")]
            public int? FadeIn = null;

            [YamlMember("fadeOut")]
            public int? FadeOut = null;

            [YamlMember("rotation")]
            public float? Rotation = null;

            [YamlMember("scale")]
            public float? Scale = null;

            [YamlMember("repeatOffset")]
            public int?[] RepeatOffset;

            public void Update(TextConfiguration another) {
                this.Color = another?.Color ?? this?.Color;
                this.FadeIn = another?.FadeIn ?? this?.FadeIn;
                this.FadeOut = another?.FadeOut ?? this?.FadeOut;
                this.Orientation = another?.Orientation ?? this?.Orientation;
                this.Padding = another?.Padding ?? this?.Padding;
                this.PaddingLine = another?.PaddingLine ?? this?.PaddingLine;
                this.PaddingSpace = another?.PaddingSpace ?? this?.PaddingSpace;
                this.Position = another?.Position ?? this?.Position;
                this.PositionOffset = another?.PositionOffset ?? this?.PositionOffset;
                this.Rotation = another?.Rotation ?? this?.Rotation;
                this.Scale = another?.Scale ?? this?.Scale;
                this.RepeatOffset = another?.RepeatOffset ?? this.RepeatOffset;
            }

            public object Clone() {
                return this.MemberwiseClone();
            }
        }

        public class TextObject {
            [YamlMember("st")]
            public int? StartTime = null;

            [YamlMember("et")]
            public int? EndTime = null;

            [YamlMember("config")]
            public TextConfiguration Config;

            [YamlMember("text")]
            public string Text = "";

            [YamlMember("texts")]
            public TextObject[] Texts;
        }

        public Vector2 Parse(TextObject obj, TextConfiguration config, FontGenerator font, Vector2 pos, int? startTime, int? endTime) 
        {
            var currentConfig = config.Clone() as TextConfiguration;
            currentConfig.Update(obj.Config);

            var times = new List<int?>();
            times.Add(0);

            if(currentConfig.RepeatOffset != null)
                times.AddRange(currentConfig.RepeatOffset.ToList());

            var subX = pos.X + (currentConfig.PositionOffset[0] ?? 0f);
            var subY = pos.Y + (currentConfig.PositionOffset[1] ?? 0f);
                    
            foreach (var t in times) 
            {
                int? st = (obj.StartTime ?? startTime) + t;
                int? et = (obj.EndTime ?? endTime) + t;

                
                if (obj.Text == "") 
                {   
                    // parse inner texts

                    Vector2 cpos = new Vector2(currentConfig.Position[0] ?? pos.X, currentConfig.Position[1] ?? pos.Y);
                    
                    foreach (var i in obj.Texts) 
                        cpos = Parse(i, currentConfig, font, cpos, st, et);

                    return new Vector2(currentConfig.Position[0] ?? pos.X, currentConfig.Position[1] ?? pos.Y);;
                } 
                else 
                {   
                    if (st == null || et == null) {
                        Log($"W: Missing startTime or endTime for sentence `{obj.Text}`");
                        return new Vector2();
                    }

                    // generate

                    subX = pos.X + (currentConfig.PositionOffset[0] ?? 0f);
                    subY = pos.Y + (currentConfig.PositionOffset[1] ?? 0f);
                    foreach (var c in obj.Text) {
                        if (c == '\n') {
                            if (currentConfig.Orientation == 0) {
                                // horizontal
                                subX = currentConfig.Position[0] ?? 0f + (currentConfig.PositionOffset[0] ?? 0f);
                                subY += currentConfig.PaddingLine[1] ?? 0f;

                            } else {
                                // vertical
                                subY = currentConfig.Position[1] ?? 0f + (currentConfig.PositionOffset[1] ?? 0f);
                                subX += currentConfig.PaddingLine[0] ?? 0f;
                            }
                        } else if (c == ' ') {
                                if (currentConfig.Orientation == 0) {
                                // horizontal
                                subX += currentConfig.PaddingSpace[0] ?? 0f;

                            } else {
                                // vertical
                                subY += currentConfig.PaddingSpace[1] ?? 0f;
                            }
                        }

                        var texture = font.GetTexture(c.ToString());
                        
                        Vector2 p = new Vector2(subX, subY);
                            //+ texture.OffsetFor(Origin) * FontScale * currentConfig.Scale.Value;

                        if (!texture.IsEmpty)
                        {
                            var sprite = GetLayer(LayerName).CreateSprite(texture.Path, Origin, p);
                            sprite.Scale(st.Value, FontScale * currentConfig.Scale.Value);
                            sprite.Fade(st.Value - currentConfig.FadeIn ?? 0, st.Value, 0, 1);
                            sprite.Fade(et.Value, et.Value + currentConfig.FadeOut ?? 0, 1, 0);
                            sprite.Rotate(st.Value, MathHelper.DegreesToRadians(currentConfig.Rotation ?? 0));
                            
                            if (currentConfig.Orientation == 0) {
                                // horizontal
                                subX += texture.Width * FontScale * currentConfig.Scale.Value;
                            } else {
                                // vertical
                                subY += texture.Height * FontScale * currentConfig.Scale.Value;
                            }
                        }

                        subX += currentConfig.Padding[0] ?? 0;
                        subY += currentConfig.Padding[1] ?? 0;
                    }
                }
            }

            return new Vector2(subX, subY);
        }

        public override void Generate()
        {
            try 
            {
                using (var file = OpenProjectFile(Path)) 
                {
                    var serializer = new SharpYaml.Serialization.Serializer();
                    TextObject obj = serializer.Deserialize<TextObject>(file);
                    
                    var configGlobal = obj.Config;

                    var font = LoadFont(SpritesPath, new FontDescription()
                    {
                        FontPath = Font,
                        FontSize = FontSize,
                        Color = FontColor,
                        Padding = new Vector2(configGlobal.Padding[0] ?? 0, configGlobal.Padding[1] ?? 0),
                        FontStyle = FontStyle
                    },
                    new FontGlow()
                    {
                        Radius = AdditiveGlow ? 0 : GlowRadius,
                        Color = GlowColor,
                    },
                    new FontOutline()
                    {
                        Thickness = OutlineThickness,
                        Color = OutlineColor,
                    },
                    new FontShadow()
                    {
                        Thickness = ShadowThickness,
                        Color = ShadowColor,
                    });

                    foreach (var i in obj.Texts) {
                        Parse(i, configGlobal.Clone() as TextConfiguration, font, new Vector2(configGlobal.Position[0] ?? 0, configGlobal.Position[1] ?? 0), null, null);
                    }
                }
            } catch (Exception e) {
                Log($"{e.Message}\n{e.StackTrace}");
            }
        }
    }
}
