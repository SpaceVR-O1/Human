
function plotrigidbodyposition( ~ , evnt )
	% The eventcallback function executs each time a frame of mocap data is delivered.
	% to Matlab. Matlab will lag if the data rate from the Host is too high.
	%A simple animated line graph displays the x, y, z position of the first rigid body in the Host.

	
	% Note - This callback uses the gobal variables x, y, z from the createlines.m script.
	% Be sure to run createline.m prior to using this function callback.
	global x
	global y
	global z
	global a1
	
	
	persistent frame1
	persistent lastframe1
	
	
	% Get the frame
	frame1 = double( evnt.data.iFrame );
	if ~isempty( frame1 ) && ~isempty( lastframe1 )
		if frame1 < lastframe1
			x.clearpoints;
			y.clearpoints;
			z.clearpoints;
		end
	end

	
	scope = 1.5;
	rbnum = 1;
	
	
	% Get the rb position
	rbx = double( evnt.data.RigidBodies( rbnum ).x ); % x position of first rb
	rby = double( evnt.data.RigidBodies( rbnum ).y ); % y position of first rb
	rbz = double( evnt.data.RigidBodies( rbnum ).z ); % z position of first rb
	
	
	% Queue the data
	frame = frame1;
	x.addpoints( frame , rbx );
	y.addpoints( frame , rby );
	z.addpoints( frame , rbz );

	
	% Collect the data in an array of max length
	%[ xinx , xiny ] = x.getpoints;
	%[ yinx , yiny ] = y.getpoints;
	%[ zinx , ziny ] = z.getpoints;

	
	% set the figure
	set( gcf , 'CurrentAxes' , a1 )

	
	% Dynamically move the axis of the graph
	axis( [ -240+frame , 20+frame , min(rbx,min(rby,rbx))-scope , max( rbx , max( rby , rbz ) )+scope ] );

	
	% Draw the data to a figure
	drawnow
	
	
	% Update lastframe
	lastframe1 = frame1;
	
	
end  % eventcallback1
