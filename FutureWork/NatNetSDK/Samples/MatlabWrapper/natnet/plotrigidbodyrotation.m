
function plotrigidbodyrotation( ~ , evnt )
	% The eventcallback function executs each time a frame of mocap data is delivered.
	% to Matlab. Matlab will lag if the data rate from the Host is too high.
	%A simple animated line graph displays the x, y, z position of the first rigid body in the Host.

	
	% Note - This callback uses the gobal variables x, y, z from the createlines.m script.
	% Be sure to run createline.m prior to using this function callback.
	global qx
	global qy
	global qz
	global qw
	global a2
	
	
	persistent frame2
	persistent lastframe2
	
	
	% Get the frame
	frame2 = double( evnt.data.iFrame );
	if ~isempty( frame2 ) && ~isempty( lastframe2 )
		if frame2 < lastframe2
			qx.clearpoints;
			qy.clearpoints;
			qz.clearpoints;
			qw.clearpoints;
		end
	end

	
	scope = 135;
	rbnum = 1;
	
	
	% Get the rb rotation
	rb = evnt.data.RigidBodies( rbnum );
	rbqx = rb.qx;
	rbqy = rb.qy;
	rbqz = rb.qz;
	rbqw = rb.qw;
	
	q = quaternion( rbqx, rbqy, rbqz, rbqw );
	qRot = quaternion( 0, 0, 0, 1);
	q = mtimes( q, qRot);
	a = EulerAngles( q , 'zyx' );
	rx = a( 1 ) * -180.0 / pi;
	ry = a( 2 ) * -180.0 / pi;
	rz = a( 3 ) * 180.0 / pi;
	

	% Queue the data
	frame = frame2;
	qx.addpoints( frame , rx );
	qy.addpoints( frame , ry );
	qz.addpoints( frame , rz );
	
	
	% Collect the data in an array of max length
	%[ xinx , xiny ] = qx.getpoints;
	%[ yinx , yiny ] = qy.getpoints;
	%[ zinx , ziny ] = qz.getpoints;
	%[ winx , winy ] = qw.getpoints;

	
	% set the figure
	set(gcf,'CurrentAxes',a2)
	
	
	% Dynamically move the axis of the graph
	axis( [ -240+frame , 20+frame , -180 , 180 ] );

	
	% Draw the data to a figure
	drawnow
	
	
	% Update lastframe
	lastframe2 = frame2;
	
	
end  % eventcallback1
