function displayrigidbodypositionandrotation( ~ , evnt )
	clc
	rbnum = 1;
	frame = evnt.data.iFrame
	rb = evnt.data.RigidBodies( rbnum );
	position = double( [ rb.x , rb.y , rb.z ] ) *100
	rbqx = rb.qx;
	rbqy = rb.qy;
	rbqz = rb.qz ;
	rbqw = rb.qw;
	
	q = quaternion( rbqx, rbqy, rbqz, rbqw );
	qRot = quaternion( 0, 0, 0, 1);
	q = mtimes( q, qRot);
	a = EulerAngles( q , 'zyx' );
	rx = a( 1 ) * -180.0 / pi;
	ry = a( 2 ) * -180.0 / pi;
	rz = a( 3 ) * 180.0 / pi;
	rotation = [ rx , ry , rz ]
end
