Multiplayer Whiteboard System (Unity + Photon Fusion)

Multiplayer Whiteboard System project, I chose to implement Photon Fusion (Shared Mode) as the networking solution within the Unity engine. This setup offers a reliable, scalable, and easy-to-integrate approach for building a real-time collaborative drawing experience across multiple platforms, including Android, WebGL, and PC.

🎯 Why Photon Fusion (Shared Mode)?

Cross-Platform Multiplayer

Photon Fusion supports seamless multiplayer interaction across mobile, browser, and desktop devices. With a single codebase, users on different platforms can draw and collaborate in real time.

Shared Simulation Mode

The whiteboard uses Fusion’s Shared Mode, where one client acts as the host and others synchronize with the host's state. This is ideal for lightweight applications like whiteboards, where real-time accuracy and simplicity matter more than full server authority.

Real-Time State Synchronization

Using Fusion’s built-in NetworkTransform and RPCs, the system synchronizes drawing strokes frame-by-frame. This ensures smooth and consistent rendering of lines across all devices.

Late Join Support

When a new player joins the session, they automatically receive the current state of the whiteboard, including all previous drawings. This is handled using Networked Lists and replayable commands for a consistent user experience.

Minimal Latency

Photon Fusion is optimized for low-latency updates, which is essential for drawing interactions that rely on precision and timing.

🔧 Technical Implementation Highlights

Drawing Synchronization: Real-time updates via RPCs and shared input handling

Late Join Handling: Networked state replay for new players to see existing drawings

Cross-Platform Build: Optimized for Android touch input, WebGL browser support, and PC mouse input

Photon Fusion Integration: Reliable state sync with simple architecture

Session Management: Host-based session with auto join/leave detection

This system can be extended for use in virtual classrooms, online collaboration tools, metaverse creative hubs, or game UI sketches, making it a flexible foundation for a variety of multiplayer experiences.
