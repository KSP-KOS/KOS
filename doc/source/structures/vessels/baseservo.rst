.. _baseservo:

BaseServo
=========

This is a special type of :struct:`PartModule` used to handle the robotic
servos that came with the Breaking Ground DLC.  If you examine a PartModule
that controls such a part, it will be this type.

This type has no extra suffixes (yet??) beyond those that come with every
:struct:`PartModuile`.  The reason it has to be a separate type is that
it needs to perform some strange work under the hood to mimic how normal
PartModules work.  (The Breaking Ground DLC PartModules have some strange
implementation changes about how their Part Action Window sliders work
that required extra work for kOS to support.)

