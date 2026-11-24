using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using KivotosMod.Runtime.Spine38;
using KivotosMod.Content.Tablet;

// Offline runtime probe only. Tools/* is excluded from the mod build/package.
public static class Arona38Probe
{
    private sealed class NoTextureLoader : TextureLoader
    {
        public void Load(AtlasPage page, string path) { }
        public void Unload(object texture) { }
    }

    public static SkeletonData Read(string root, string stem = "arona")
    {
        var atlas = new Atlas(new StringReader(File.ReadAllText(Path.Combine(root, stem + ".atlas.bin"))), root, new NoTextureLoader());
        var reader = new SkeletonBinary(atlas) { AllowLegacy3875 = true };
        using (var stream = File.OpenRead(Path.Combine(root, stem + ".skel.bin")))
        {
            SkeletonData data = reader.ReadSkeletonData(stream);
            if (stream.Position != stream.Length) throw new Exception("Skeleton not fully consumed");
            return data;
        }
    }

    public static string Inspect(string root, string stem = "arona")
    {
        SkeletonData data = Read(root, stem);
        return JsonSerializer.Serialize(new {
            version = data.Version,
            bones = data.Bones.Select(b => b.Name).ToArray(),
            slots = data.Slots.Select(s => s.Name).ToArray(),
            animations = data.Animations.Select(a => new {
                name = a.Name, duration = a.Duration,
                timelines = a.Timelines.Select(t => new {
                    type = t.GetType().Name,
                    target = t is IBoneTimeline bone ? data.Bones.Items[bone.BoneIndex].Name
                        : t is ISlotTimeline slot ? data.Slots.Items[slot.SlotIndex].Name : "",
                    propertyId = t.PropertyId,
                    frames = t is TranslateTimeline translate ? translate.Frames : t is RotateTimeline rotate ? rotate.Frames : null
                }).ToArray()
            }).ToArray()
        });
    }

    public static string Pose(string root, float seconds, string stem = "arona", float mouseX = 0, float mouseY = 0, bool follow = false, float controlX = 0, float controlY = 0)
    {
        var animation = new TabletSpine38Animation(Read(root, stem), new Random(1), stem == "sora", stem == "plana");
        if (follow) { animation.LookAt(mouseX, mouseY); for (int i = 0; i < 60; i++) animation.Update(1f / 60); }
        if (seconds > 0) animation.Blink();
        for (float t = 0; t < seconds; t += 1f / 60) animation.Update(Math.Min(1f / 60, seconds - t));
        var skeleton = animation.Skeleton;
        if (controlX != 0 || controlY != 0) {
            Bone control = skeleton.FindBone(stem == "plana" ? "face2" : "bone18");
            control.X += controlX; control.Y += controlY; skeleton.UpdateWorldTransform();
        }
        float[] buffer = null;
        skeleton.GetBounds(out float x, out float y, out float width, out float height, ref buffer);
        var meshes = skeleton.DrawOrder.Select(slot => {
            float[] vertices, uvs;
            int[] triangles;
            string name;
            if (slot.Attachment is RegionAttachment region)
            {
                vertices = new float[8]; region.ComputeWorldVertices(slot.Bone, vertices, 0, 2);
                uvs = region.UVs; triangles = new[] { 0, 1, 2, 2, 3, 0 }; name = region.Name;
            }
            else if (slot.Attachment is MeshAttachment mesh)
            {
                vertices = new float[mesh.WorldVerticesLength]; mesh.ComputeWorldVertices(slot, 0, vertices.Length, vertices, 0, 2);
                uvs = mesh.UVs; triangles = mesh.Triangles; name = mesh.Name;
            }
            else return null;
            animation.ApplyEyeTracking(slot.Attachment, vertices, vertices.Length);
            return new { attachment = name, texture = stem + "38.png", vertices, uvs, triangles, color = new { a = slot.A * animation.EyeOpacity(slot.Attachment) } };
        }).Where(m => m != null).ToArray();
        return JsonSerializer.Serialize(new { bounds = new { x, y, width, height }, meshes });
    }

    public static string Validate(string root, string stem = "arona")
    {
        SkeletonData data = Read(root, stem);
        var animation = new TabletSpine38Animation(data, new Random(1337), stem == "sora", stem == "plana");
        var reference = new Skeleton(data);
        var referenceState = new AnimationState(new AnimationStateData(data));
        referenceState.SetAnimation(0, stem != "arona" ? "animation" : "swing", true);
        int blinks = 0, meshes = 0;
        float now = 0, previousEnd = -1, minGap = float.PositiveInfinity, maxGap = 0;
        bool wasBlinking = false;
        for (int frame = 0; frame < 3600; frame++)
        {
            now += 1f / 60;
            animation.Update(1f / 60);
            referenceState.Update(1f / 60); referenceState.Apply(reference); reference.UpdateWorldTransform();
            if (animation.Blinking && !wasBlinking)
            {
                blinks++;
                if (previousEnd >= 0)
                {
                    float gap = now - previousEnd;
                    minGap = Math.Min(minGap, gap); maxGap = Math.Max(maxGap, gap);
                    if (gap < 3f - 0.04f || gap > 6f + 0.04f) throw new Exception("Blink interval outside 3-6 seconds");
                }
            }
            if (!animation.Blinking && wasBlinking) previousEnd = now;
            wasBlinking = animation.Blinking;
            for (int i = 0; i < reference.Bones.Count; i++)
            {
                Bone b = animation.Skeleton.Bones.Items[i], r = reference.Bones.Items[i];
                if (!TabletSpine38Animation.IsEyeBone(b.Data.Name, stem == "sora", stem == "plana"))
                {
                    Near(b.X, r.X); Near(b.Y, r.Y); Near(b.Rotation, r.Rotation);
                    Near(b.ScaleX, r.ScaleX); Near(b.ScaleY, r.ScaleY);
                }
            }
            foreach (Slot slot in animation.Skeleton.DrawOrder)
            {
                float[] vertices; int[] triangles; float[] uvs;
                if (slot.Attachment is RegionAttachment region)
                {
                    vertices = new float[8]; region.ComputeWorldVertices(slot.Bone, vertices, 0, 2);
                    triangles = new[] { 0, 1, 2, 2, 3, 0 }; uvs = region.UVs;
                }
                else if (slot.Attachment is MeshAttachment mesh)
                {
                    vertices = new float[mesh.WorldVerticesLength]; mesh.ComputeWorldVertices(slot, 0, vertices.Length, vertices, 0, 2);
                    triangles = mesh.Triangles; uvs = mesh.UVs;
                }
                else continue;
                if (vertices.Any(v => !float.IsFinite(v)) || uvs.Any(v => !float.IsFinite(v))
                    || triangles.Any(t => t < 0 || t >= vertices.Length / 2)) throw new Exception("Invalid mesh");
                if (slot.A <= 0) throw new Exception("Body slot disappeared");
                meshes++;
            }
        }
        if (blinks < 5) throw new Exception("Automatic blink did not play");
        // Finish any current blink, trigger a manual blink, then allow it to fully fade.
        for (int i = 0; i < 60; i++) animation.Update(1f / 60);
        animation.Blink();
        for (int i = 0; i < 12; i++) { animation.Blink(); animation.Update(1f / 60); }
        float closedScale = animation.Skeleton.FindBone(stem == "plana" ? "eyelash2" : stem == "sora" ? "eyebow2" : "eyes2").ScaleX;
        if (closedScale > (stem != "arona" ? 0.85f : 0.6f)) throw new Exception("Eye did not close");
        for (int i = 0; i < 60; i++) animation.Update(1f / 60);
        foreach (Bone bone in animation.Skeleton.Bones)
            if (TabletSpine38Animation.IsEyeBone(bone.Data.Name, stem == "sora", stem == "plana"))
            {
                Near(bone.X, bone.Data.X); Near(bone.Y, bone.Data.Y); Near(bone.Rotation, bone.Data.Rotation);
                Near(bone.ScaleX, bone.Data.ScaleX); Near(bone.ScaleY, bone.Data.ScaleY);
            }
        // Creating the isolated blink must not rewrite the user's original animation.
        var originalEye = data.FindAnimation("blink").Timelines.OfType<ScaleTimeline>().First(t => data.Bones.Items[t.BoneIndex].Name == (stem == "plana" ? "eyelash2" : stem == "sora" ? "eyebow2" : "eyes2"));
        Near(originalEye.Frames[0], stem != "arona" ? 1.166667f : 1f);
        return JsonSerializer.Serialize(new { passed = true, version = data.Version, frames = 3600,
            blinks, minGap, maxGap, meshes, blinkDuration = animation.BlinkDuration,
            bodyIdleUnchanged = true, eyesCloseAndRestore = true, originalTimelinesPreserved = true });
    }

    public static string ValidateTracking(string root, string stem)
    {
        var data = Read(root, stem);
        var animation = new TabletSpine38Animation(data, new Random(1), stem == "sora", stem == "plana");
        float[] MeshVertices(Slot slot)
        {
            if (slot.Attachment is MeshAttachment mesh)
            {
                var vertices = new float[mesh.WorldVerticesLength];
                mesh.ComputeWorldVertices(slot, 0, vertices.Length, vertices, 0, 2);
                return vertices;
            }
            var region = (RegionAttachment)slot.Attachment;
            var quad = new float[8]; region.ComputeWorldVertices(slot.Bone, quad, 0, 2); return quad;
        }
        float Offset(float x, float y, bool horizontal)
        {
            animation.LookAt(x, y);
            for (int i = 0; i < 120; i++) animation.Update(1f / 60);
            // Let any automatic blink finish before inspecting the tracking layer.
            while (animation.Blinking) animation.Update(1f / 60);
            float result = 0;
            foreach (Slot slot in animation.Skeleton.Slots)
            {
                float[] source = MeshVertices(slot), tracked = (float[])source.Clone();
                animation.ApplyEyeTracking(slot.Attachment, tracked, tracked.Length);
                bool pupils = slot.Attachment.Name == (stem == "plana" ? "pupil" : "Hitomi");
                for (int i = 0; i < tracked.Length; i++)
                {
                    float difference = tracked[i] - source[i];
                    if (!pupils) Near(difference, 0);
                    else {
                        if (Math.Abs(difference) > (i % 2 == 0 ? 2.501f : 1.401f)) throw new Exception("Eye tracking out of bounds");
                        if (i % 2 == (horizontal ? 0 : 1)) result = difference;
                    }
                }
            }
            return result;
        }
        float right = Offset(10000, 10000, true), left = Offset(-10000, -10000, true);
        float up = Offset(10000, 10000, false), down = Offset(-10000, -10000, false);
        if (right < 2.4f || left > -2.4f || up < 1.3f || down > -1.3f) throw new Exception("Eye tracking direction is incorrect");
        int blinkChecks = 0;
        // Regression: a blink must keep the last gaze offset, including its very
        // first rendered frame. Moving the pointer during closure resumes smoothly
        // only after the blink, rather than snapping the pupils to the middle.
        foreach (bool automatic in new[] { false, true })
        foreach (int direction in new[] { -1, 1 })
        {
            var gaze = new TabletSpine38Animation(data, new Random(1), stem == "sora", stem == "plana");
            Slot pupilSlot = gaze.Skeleton.Slots.First(slot => slot.Attachment.Name == (stem == "plana" ? "pupil" : "Hitomi"));
            float[] Displacement()
            {
                float[] source = MeshVertices(pupilSlot), tracked = (float[])source.Clone();
                gaze.ApplyEyeTracking(pupilSlot.Attachment, tracked, tracked.Length);
                return new[] { tracked[0] - source[0], tracked[1] - source[1] };
            }
            gaze.LookAt(direction * 10000, direction * 10000);
            for (int i = 0; i < 90; i++) gaze.Update(1f / 60);
            if (automatic) {
                for (int i = 0; !gaze.Blinking && i < 600; i++) gaze.Update(1f / 60);
                if (!gaze.Blinking) throw new Exception("Automatic blink missing during gaze test");
            }
            float[] before = Displacement();
            if (Math.Abs(before[0]) < 2.4f || Math.Abs(before[1]) < 1.3f)
                throw new Exception("Blink snapped gaze to the middle");
            if (!automatic) gaze.Blink();
            if (!automatic) Near(gaze.EyeOpacity(pupilSlot.Attachment), 1);
            else if (gaze.EyeOpacity(pupilSlot.Attachment) < 0.75f)
                throw new Exception("Iris disappeared at the blink trigger");
            float[] triggered = Displacement();
            Near(triggered[0], before[0]); Near(triggered[1], before[1]);
            gaze.LookAt(-direction * 10000, -direction * 10000);
            float elapsed = 0;
            bool stayedOpaque = true;
            while (gaze.Blinking)
            {
                float[] offset = Displacement();
                Near(offset[0], before[0]); Near(offset[1], before[1]);
                // Lids, whites and body still receive the unmodified source pose.
                foreach (Slot slot in gaze.Skeleton.Slots.Where(slot => slot != pupilSlot))
                {
                    float[] source = MeshVertices(slot), tracked = (float[])source.Clone();
                    gaze.ApplyEyeTracking(slot.Attachment, tracked, tracked.Length);
                    for (int i = 0; i < tracked.Length; i++) Near(tracked[i], source[i]);
                }
                gaze.Update(1f / 60);
                elapsed += 1f / 60;
                if (elapsed >= 0.2f && elapsed <= 0.25f)
                    stayedOpaque &= Math.Abs(gaze.EyeOpacity(pupilSlot.Attachment) - 1f) < 0.0001f;
            }
            if (!stayedOpaque) throw new Exception("Blink changed pupil opacity");
            Near(gaze.EyeOpacity(pupilSlot.Attachment), 1);
            float[] reopening = Displacement();
            if (Math.Abs(reopening[0] - before[0]) > 1 || Math.Abs(reopening[1] - before[1]) > 0.6f)
                throw new Exception("Gaze snapped on reopening");
            for (int i = 0; i < 90; i++) gaze.Update(1f / 60);
            float[] resumed = Displacement();
            if (resumed[0] * direction > -2.4f || resumed[1] * direction > -1.3f)
                throw new Exception("Gaze failed to resume following the pointer");
            blinkChecks++;
        }
        return JsonSerializer.Serialize(new { passed = true, model = stem, right, left, up, down,
            nonPupilMeshesUnchanged = true, blinkGazeChecks = blinkChecks,
            blinkGazePreserved = true, pupilRemainsOpaque = true, smoothResume = true, boundedWithoutAccumulation = true });
    }

    public static string ValidateHeadTracking(string root, string stem = "arona")
    {
        bool plana = stem == "plana";
        string controlName = plana ? "face2" : "bone18";
        SkeletonData data = Read(root, stem);
        int index = data.FindBone(controlName).Index;
        float minX = 0, maxX = 0, minY = 0, maxY = 0;
        if (plana) {
            minX = -TabletSpine38Animation.PlanaTurnRangeX; maxX = -minX;
            minX *= TabletSpine38Animation.PlanaRightTurnScale;
            minY = -TabletSpine38Animation.PlanaTurnRangeY; maxY = -minY;
        }
        else {
            var range = data.FindAnimation("Turning").Timelines.OfType<TranslateTimeline>()
                .First(t => t is not ScaleTimeline && t.BoneIndex == index).Frames;
            for (int i = 0; i < range.Length; i += 3) {
                minX = Math.Min(minX, range[i + 1]); maxX = Math.Max(maxX, range[i + 1]);
                minY = Math.Min(minY, range[i + 2]); maxY = Math.Max(maxY, range[i + 2]);
            }
            maxX *= TabletSpine38Animation.AronaLeftTurnScale;
        }
        float[] Focus(TabletSpine38Animation animation)
        {
            Bone control = animation.Skeleton.FindBone(controlName);
            float x = control.X - control.Data.X, y = control.Y - control.Data.Y;
            if (plana) {
                float worldX = control.Parent.A * x + control.Parent.B * y;
                y = control.Parent.C * x + control.Parent.D * y;
                x = worldX;
            }
            if (x < minX - 0.001f || x > maxX + 0.001f || y < minY - 0.001f || y > maxY + 0.001f)
                throw new Exception("Head left the authored control range");
            return new[] { x <= 0 ? x / minX : -x / maxX, y <= 0 ? y / minY : -y / maxY };
        }
        float[] Center(Skeleton skeleton)
        {
            Slot face = skeleton.Slots.First(slot => slot.Attachment.Name == "face");
            var mesh = (MeshAttachment)face.Attachment; var vertices = new float[mesh.WorldVerticesLength];
            mesh.ComputeWorldVertices(face, 0, vertices.Length, vertices, 0, 2);
            return new[] { vertices.Where((_, i) => i % 2 == 0).Average(), vertices.Where((_, i) => i % 2 != 0).Average() };
        }
        var movements = new System.Collections.Generic.List<object>();
        float rightTravel = 0, leftTravel = 0;
        foreach (var direction in new[] { new[] { 10000f, 0f }, new[] { -10000f, 0f }, new[] { 0f, 10000f }, new[] { 0f, -10000f } })
        {
            var animation = new TabletSpine38Animation(data, new Random(1337), plana: plana);
            var reference = new Skeleton(data); var idle = new AnimationState(new AnimationStateData(data));
            idle.SetAnimation(0, plana ? "animation" : "swing", true); idle.Apply(reference); reference.UpdateWorldTransform();
            float[] center = Center(reference);
            animation.LookAt(center[0] + direction[0], center[1] + direction[1]);
            for (int frame = 0; frame < 90; frame++) {
                animation.Update(1f / 60); idle.Update(1f / 60); idle.Apply(reference); reference.UpdateWorldTransform();
                Focus(animation);
                for (int b = 0; b < data.Bones.Count; b++) {
                    Bone actual = animation.Skeleton.Bones.Items[b], baseline = reference.Bones.Items[b];
                    if (b == index || TabletSpine38Animation.IsEyeBone(actual.Data.Name, false, plana)) continue;
                    Near(actual.X, baseline.X); Near(actual.Y, baseline.Y); Near(actual.Rotation, baseline.Rotation);
                    Near(actual.ScaleX, baseline.ScaleX); Near(actual.ScaleY, baseline.ScaleY);
                }
                foreach (Slot slot in animation.Skeleton.Slots) {
                    if (slot.Attachment is not MeshAttachment mesh) continue;
                    var vertices = new float[mesh.WorldVerticesLength]; mesh.ComputeWorldVertices(slot, 0, vertices.Length, vertices, 0, 2);
                    animation.ApplyEyeTracking(mesh, vertices, vertices.Length);
                    if (vertices.Any(v => !float.IsFinite(v))) throw new Exception("Head pose produced invalid mesh vertices");
                    Near(animation.EyeOpacity(mesh), 1);
                }
            }
            float[] turned = Center(animation.Skeleton), straight = Center(reference);
            float deltaX = turned[0] - straight[0], deltaY = turned[1] - straight[1];
            int axis = direction[0] != 0 ? 0 : 1;
            if ((axis == 0 ? deltaX : deltaY) * direction[axis] <= 0) throw new Exception("Head turned away from the pointer");
            if (axis == 0) {
                if (direction[0] > 0) rightTravel = deltaX;
                else leftTravel = -deltaX;
            }
            movements.Add(new { mouseX = direction[0], mouseY = direction[1], deltaX, deltaY });
        }
        if (plana && Math.Abs(rightTravel / leftTravel - TabletSpine38Animation.PlanaRightTurnScale) > 0.002f)
            throw new Exception("Plana's right turn was not reduced independently of the left turn");
        var rateChecks = new System.Collections.Generic.List<object>();
        float? previousResponse = null;
        foreach (int fps in new[] { 30, 60, 144 })
        {
            var animation = new TabletSpine38Animation(data, new Random(1337), plana: plana);
            animation.LookAt(10000, 10000);
            float[] previous = Focus(animation);
            float maxSpeed = 0;
            for (int frame = 0; frame < fps / 2; frame++) {
                animation.Update(1f / fps); float[] current = Focus(animation);
                for (int axis = 0; axis < 2; axis++) maxSpeed = Math.Max(maxSpeed, Math.Abs(current[axis] - previous[axis]) * fps);
                previous = current;
            }
            float response = previous[0];
            if (response < 0.8f || response >= 1) throw new Exception("Head tracking response is too slow or snaps to the pointer");
            if (previousResponse.HasValue && Math.Abs(response - previousResponse.Value) > 0.025f) throw new Exception("Tracking depends excessively on frame rate");
            previousResponse = response;
            float maxOscillation = 0;
            for (int frame = 0; frame < fps * 5; frame++) {
                float sign = frame % 2 == 0 ? -1 : 1; animation.LookAt(sign * 10000, sign * 10000);
                animation.Update(1f / fps); float[] current = Focus(animation);
                for (int axis = 0; axis < 2; axis++) {
                    maxSpeed = Math.Max(maxSpeed, Math.Abs(current[axis] - previous[axis]) * fps);
                    if (frame > fps * 2) maxOscillation = Math.Max(maxOscillation, Math.Abs(current[axis]));
                }
                previous = current;
            }
            if (maxSpeed > TabletSpine38Animation.MaxTrackingSpeed + 0.001f || maxOscillation > 0.12f)
                throw new Exception("Rapid pointer movement shakes the head excessively: fps=" + fps + ", speed=" + maxSpeed + ", amplitude=" + maxOscillation);
            rateChecks.Add(new { fps, responseAtHalfSecond = response, maxSpeed, maxOscillation });
        }
        return JsonSerializer.Serialize(new { passed = true, model = stem, controlBone = controlName, drivenControlCount = 1,
            responseSeconds = TabletSpine38Animation.TrackingResponseSeconds, minX, maxX, minY, maxY,
            followsPointerDirection = true, bodyIdleUnchanged = true, finiteMeshes = true,
            singleFilteredFocus = true, pupilRemainsOpaque = true, movements, rateChecks });
    }

    private static void Near(float actual, float expected)
    {
        if (Math.Abs(actual - expected) > 0.0001f) throw new Exception("Unexpected pose: " + actual + " / " + expected);
    }

    public static string ValidateUiClock(string root, Func<long, long, float> delta, int fps, string stem = "arona")
    {
        var animation = new TabletSpine38Animation(Read(root, stem), new Random(1337), stem == "sora", stem == "plana");
        float[] setupRotation = animation.Skeleton.Bones.Select(b => b.Rotation).ToArray();
        float bodyMotion = 0;
        int autoBlinks = 0;
        bool wasBlinking = false;
        long previous = 0;
        for (int frame = 0; frame < fps * 30; frame++)
        {
            long current = 1 + (long)((frame + 1) * (double)System.Diagnostics.Stopwatch.Frequency / fps);
            // The host GameTime elapsed is zero in this regression scenario. UI time
            // must still advance and trigger blinking without any click/Blink call.
            animation.Update(delta(previous, current));
            previous = current;
            if (animation.Blinking && !wasBlinking) autoBlinks++;
            wasBlinking = animation.Blinking;
            for (int i = 0; i < setupRotation.Length; i++)
            {
                Bone bone = animation.Skeleton.Bones.Items[i];
                if (!TabletSpine38Animation.IsEyeBone(bone.Data.Name, stem == "sora", stem == "plana"))
                    bodyMotion = Math.Max(bodyMotion, Math.Abs(bone.Rotation - setupRotation[i]));
            }
        }
        if (autoBlinks < 3 || bodyMotion < 0.05f) throw new Exception("UI idle/blink did not advance");
        if (delta(0, previous + System.Diagnostics.Stopwatch.Frequency * 300) != 0)
            throw new Exception("Reopening should reset the UI clock");
        return JsonSerializer.Serialize(new { passed = true, fps, simulationSeconds = 30,
            hostElapsedZero = true, clickCount = 0, autoBlinks, bodyMotion });
    }
}
