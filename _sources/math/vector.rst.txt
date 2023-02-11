.. _vectors:

Vectors
=======

.. contents:: Contents
    :local:
    :depth: 1

Creation
--------

.. function:: V(x,y,z)

    :parameter x: (scalar) :math:`x` coordinate
    :parameter y: (scalar) :math:`y` coordinate
    :parameter z: (scalar) :math:`z` coordinate
    :return: :struct:`Vector`

    This creates a new vector from 3 components in :math:`(x,y,z)`::

        SET vec TO V(x,y,z).

    Here, a new :struct:`Vector` called ``vec`` is created . The object :struct:`Vector` represents a `three-dimensional euclidean vector <http://en.wikipedia.org/wiki/Euclidean_vector>`__ To deeply understand most vectors in kOS, you have to understand a bit about the :ref:`underlying coordinate system of KSP <ref frame>`. If you are having trouble making sense of the direction the axes point in, go read that page.

.. note::
    Remember that the XYZ grid in Kerbal Space Program uses a
    :ref:`left-handed <left-handed>` coordinate system.

Structure
---------

.. structure:: Vector

    .. list-table:: **Members**
        :widths: 4 2 1 1
        :header-rows: 1

        * - Suffix
          - Type
          - Get
          - Set

        * - :attr:`X`
          - :struct:`Scalar`
          - yes
          - yes
        * - :attr:`Y`
          - :struct:`Scalar`
          - yes
          - yes
        * - :attr:`Z`
          - :struct:`Scalar`
          - yes
          - yes
        * - :attr:`MAG`
          - :struct:`Scalar`
          - yes
          - yes
        * - :attr:`NORMALIZED`
          - :struct:`Vector`
          - yes
          - no
        * - :attr:`SQRMAGNITUDE`
          - :struct:`Scalar`
          - yes
          - no
        * - :attr:`DIRECTION`
          - :struct:`Direction`
          - yes
          - yes
        * - :attr:`VEC`
          - :struct:`Vector`
          - yes
          - no

.. note::

    This type is serializable.

.. attribute:: Vector:X

    :type: :struct:`Scalar`
    :access: Get/Set

    The :math:`x` component of the vector.

.. attribute:: Vector:Y

    :type: :struct:`Scalar`
    :access: Get/Set

    The :math:`y` component of the vector.

.. attribute:: Vector:Z

    :type: :struct:`Scalar`
    :access: Get/Set

    The :math:`z` component of the vector.

.. attribute:: Vector:MAG

    :type: :struct:`Scalar`
    :access: Get/Set

    The magnitude of the vector, as a scalar number, by the Pythagorean Theorem.

.. attribute:: Vector:NORMALIZED

    :type: :struct:`Vector`
    :access: Get only

    This creates a unit vector pointing in the same direction as this vector. This is the same effect as multiplying the vector by the scalar ``1 / vec:MAG``.

.. attribute:: Vector:SQRMAGNITUDE

    :type: :struct:`Scalar`
    :access: Get only

    The magnitude of the vector, squared. Use instead of ``vec:MAG^2`` if you need to square of the magnitude as this skips the step in the Pythagorean formula where you take the square root in the first place. Taking the square root and then squaring that would introduce floating point error needlessly.

.. attribute:: Vector:DIRECTION

    :type: :struct:`Direction`
    :access: Get/Set

    GET:
        The vector rendered into a :ref:`Direction <direction>` (see
        :ref:`note in the Directions documentation <vectors_vs_directions>`
        about information loss when doing this).

    SET:
        Tells the vector to keep its magnitude as it is but point in a new direction, adjusting its :math:`(x,y,z)` numbers accordingly.

.. attribute:: Vector:VEC

    :type: :struct:`Vector`
    :access: Get only

    This is a suffix that creates a *COPY* of this vector. Useful if you want to copy a vector and then change the copy. Normally if you ``SET v2 TO v1``, then ``v1`` and ``v2`` are two names for the same vector and changing one would change the other.


Operations and Methods
----------------------

======================================================================== =============
Method / Operator                                                         Return Type
======================================================================== =============
 :ref:`* (asterisk) <Vector *>`                                          :struct:`Scalar` or :struct:`Vector`
 :ref:`+ (plus)     <Vector +->`                                         :struct:`Vector`
 :ref:`- (minus)    <Vector +->`                                         :struct:`Vector`
 :ref:`- (unary)    <Vector +->`                                         :struct:`Vector`
 :func:`VDOT`, :func:`VECTORDOTPRODUCT`, :ref:`* (asterisk) <Vector *>`  :struct:`Scalar`
 :func:`VCRS`, :func:`VECTORCROSSPRODUCT`                                :struct:`Vector`
 :func:`VANG`, :func:`VECTORANGLE`                                       :struct:`Scalar` (deg)
 :func:`VXCL`, :func:`VECTOREXCLUDE`                                     :struct:`Vector`
======================================================================== =============

.. index:: vector multiplication
.. _Vector *:
.. object:: *

    `Scalar multiplication <https://mathinsight.org/vector_introduction#scalarmultiplication>`__ or
    `dot product <https://mathinsight.org/dot_product>`__
    of two ``Vectors``. See also :func:`VECTORDOTPRODUCT`::

        SET a TO 2.
        SET vec1 TO V(1,2,3).
        SET vec2 TO V(2,3,4).
        PRINT a * vec1.     // prints: V(2,4,6)
        PRINT vec1 * vec2.  // prints: 20

    Note that the *unary* minus operator is really a multiplication of
    the vector by a scalar of (-1)::

	PRINT -vec1.     // these two both print the
	PRINT (-1)*vec1. // exact same thing.

.. index:: vector addition
.. index:: vector subtraction
.. _Vector +-:
.. object:: +, -

    `Adding <https://mathinsight.org/vector_introduction#addition>`__ and `subtracting <https:/mathinsight.org/vector_introduction#subtraction>`__ a :struct:`Vector` with another :struct:`Vector`::

        SET a TO 2.
        SET vec1 TO V(1,2,3).
        SET vec2 TO V(2,3,4).
        PRINT vec1 + vec2.  // prints: V(3,5,7)
        PRINT vec2 - vec1.  // prints: V(1,1,1)

    Note that the *unary* minus operator is the same thing as multiplying
    the vector by a scalar of (-1), and is not technically an addition or
    subtraction operator::

        // These two both print the same exact thing:
	PRINT -vec1.
	PRINT (-1)*vec1.

.. function:: VDOT(v1,v2)

    Same as :func:`VECTORDOTPRODUCT(v1,v2)` and :ref:`v1 * v2 <Vector *>`.

.. function:: VECTORDOTPRODUCT(v1,v2)

    :parameter v1: (:struct:`Vector`)
    :parameter v2: (:struct:`Vector`)
    :return: The `vector dot-product <https://mathinsight.org/dot_product>`__
    :rtype: :struct:`Scalar`

    This is the `dot product <https://mathinsight.org/dot_product>`__ of two vectors returning a scalar number. This is the same as :ref:`v1 * v2 <Vector *>`::

        SET vec1 TO V(1,2,3).
        SET vec2 TO V(2,3,4).

        // These are different ways to perform the same operation.
        // All of them will print the value: 20
        // -------------------------------------------------------
        PRINT VDOT(vec1, vec2).
        PRINT VECTORDOTPRODUCT(vec1, vec2).
        PRINT vec1 * vec2. // multiplication of two vectors with asterisk "*" performs a VDOT().

.. function:: VCRS(v1,v2)

    Same as :func:`VECTORCROSSPRODUCT(v1,v2)`

.. function:: VECTORCROSSPRODUCT(v1,v2)

    :parameter v1: (:struct:`Vector`)
    :parameter v2: (:struct:`Vector`)
    :return: The `vector cross-product <https://mathinsight.org/cross_product>`__
    :rtype: :struct:`Vector`

    The vector `cross product <https://mathinsight.org/cross-product/>`__ of two vectors in the order ``(v1,v2)`` returning a new `Vector`::

        SET vec1 TO V(1,2,3).
        SET vec2 TO V(2,3,4).

        // These will both print: V(-1,2,-1)
        PRINT VCRS(vec1, vec2).
        PRINT VECTORCROSSPRODUCT(vec1, vec2).

    When visualizing the direction that a vector cross product will
    point, remember that KSP is using a :ref:`left-handed <left-handed>`
    coordinate system, and this means a cross-product of two vectors
    will point in the opposite direction of what it would had KSP been
    using a right-handed coordinate system.

.. function:: VANG(v1,v2)::

    Same as :func:`VECTORANGLE(v1,v2)`.

.. function:: VECTORANGLE(v1,v2)

    :parameter v1: (:struct:`Vector`)
    :parameter v2: (:struct:`Vector`)
    :return: Angle between two vectors
    :rtype: :struct:`Scalar`

    This returns the angle between v1 and v2. It is the same result as:

    .. math::

        \arccos\left(
            \frac{
                \vec{v_1}\cdot\vec{v_2}
            }{
                \left|\vec{v_1}\right|\cdot\left|\vec{v_2}\right|
            }
        \right)

    or in **KerboScript**::

        arccos( (VDOT(v1,v2) / (v1:MAG * v2:MAG) ) )

.. function:: VXCL(v1,v2)

    Same as :func:`VECTOREXCLUDE(v1,v2)`

.. function:: VECTOREXCLUDE(v1,v2)

    This is a vector, ``v2`` with all of ``v1`` excluded from it. In other words, the projection of ``v2`` onto the plane that is normal to ``v1``.

Some examples of using the :struct:`Vector` object::

    // initializes a vector with x=100, y=5, z=0
    SET varname TO V(100,5,0).

    varname:X.    // Returns 100.
    V(100,5,0):Y. // Returns 5.
    V(100,5,0):Z. // Returns 0.

    // Returns the magnitude of the vector
    varname:MAG.

    // Changes x coordinate value to 111.
    SET varname:X TO 111.

    // Lengthen or shorten vector to make its magnitude 10.
    SET varname:MAG to 10.

    // get vector pointing opposite to surface velocity.
    SET retroSurf to (-1)*velocity:surface.

    // use cross product to find normal to the orbit plane.
    SET norm to VCRS(velocity:orbit, ship:body:position).
