global hf1 a1 a2 x y z qx qy qz qw
hf1 = figure;
hf1.WindowStyle = 'docked';
a1 = subplot(1,2,1);

x=animatedline;
x.MaximumNumPoints = 1000;
x.Marker = '.';
x.LineWidth = 0.5;
x.Color = [ 1 0 0];

y=animatedline;
y.MaximumNumPoints = 1000;
y.LineWidth = 0.5;
y.Color = [ 0 1 0];
y.Marker = '.';

z=animatedline;
z.MaximumNumPoints = 1000;
z.LineWidth = 0.5;
z.Color = [ 0 0 1];
z.Marker = '.';

a2 = subplot(1,2,2);

qx=animatedline;
qx.MaximumNumPoints = 1000;
qx.Marker = '.';
qx.LineWidth = 0.5;
qx.Color = [ 1 0 0 ];

qy=animatedline;
qy.MaximumNumPoints = 1000;
qy.Marker = '.';
qy.LineWidth = 0.5;
qy.Color = [ 0 1 0 ];

qz=animatedline;
qz.MaximumNumPoints = 1000;
qz.Marker = '.';
qz.LineWidth = 0.5;
qz.Color = [ 0 0 1];

qw=animatedline;
qw.MaximumNumPoints = 1000;
qw.Marker = '.';
qw.LineWidth = 0.5;
qw.Color = [ 0 1 1];

c = natnet;
%c.ClientIP = '127.0.0.1';
%c.HostIP = '127.0.0.1';
%c.ConnectionType = 'Multicast';
c.connect

c.addlistener( 1 , 'plotrigidbodyposition' );
c.addlistener( 2 , 'plotrigidbodyrotation' );
c.addlistener( 3 , 'displayrigidbodypositionandrotation' );

c.enable(0)
