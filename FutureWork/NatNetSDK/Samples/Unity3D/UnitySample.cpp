//=============================================================================
// Copyright © NaturalPoint, Inc. All Rights Reserved.
// 
// This software is provided by the copyright holders and contributors "as is" and
// any express or implied warranties, including, but not limited to, the implied
// warranties of merchantability and fitness for a particular purpose are disclaimed.
// In no event shall NaturalPoint, Inc. or contributors be liable for any direct,
// indirect, incidental, special, exemplary, or consequential damages
// (including, but not limited to, procurement of substitute goods or services;
// loss of use, data, or profits; or business interruption) however caused
// and on any theory of liability, whether in contract, strict liability,
// or tort (including negligence or otherwise) arising in any way out of
// the use of this software, even if advised of the possibility of such damage.
//=============================================================================


/*

UnitySample.cpp

This program connects to a NatNet server, receives a data stream, encodes a skeleton to XML, and
outputs XML locally over UDP to Unity.  The purpose is to illustrate how to get data into Unity3D.

Usage [optional]:

	UnitySample [ServerIP] [LocalIP] [Unity3D IP]

	[ServerIP]			IP address of the server (e.g. 192.168.0.107) ( defaults to local machine)
*/

#include <stdio.h>
#include <tchar.h>
#include <conio.h>
#include <winsock2.h>
#include <string>
#include <sstream>
#include <vector>
#include <map>

#include "NatNetTypes.h"
#include "NatNetClient.h"

#include "NatNetRepeater.h"   //== for transport of data over UDP to Unity3D

//== Slip Stream globals ==--

cSlipStream *gSlipStream;
std::map<int, std::string> gBoneNames;

#pragma warning( disable : 4996 )

void __cdecl DataHandler(sFrameOfMocapData* data, void* pUserData);		// receives data from the server
void __cdecl MessageHandler(int msgType, char* msg);		            // receives NatNet error mesages
void resetClient();
int CreateClient(int iConnectionType);

unsigned int MyServersDataPort = 3130;
unsigned int MyServersCommandPort = 3131;

NatNetClient* theClient;
FILE* fp;

char szMyIPAddress[128] = "";
char szServerIPAddress[128] = "";
char szUnityIPAddress[128] = "";

//== Helpers for text indicator of data flowing... ==--

enum eStatusIndicator
{
    Uninitialized = 0,
    Listening,
    Streaming
};

eStatusIndicator gIndicator;

double gIndicatorTimer = 0;
double gFrequency = 0;

long long getTickCount()
{
    long long   curTime;
    QueryPerformanceCounter( reinterpret_cast<LARGE_INTEGER*>( &curTime ) );
    return curTime;
}

void getFrequency()
{
    if( gFrequency==0 )
    {
        LARGE_INTEGER freq;
        QueryPerformanceFrequency( &freq );
        gFrequency = double( freq.QuadPart );

        gIndicatorTimer = (double) getTickCount();
    }
}

double elapsedTimer()
{
    getFrequency();
    long long   curTime = getTickCount();
    return (double) ( curTime - gIndicatorTimer ) / gFrequency;
}

void catchUp()
{
    gIndicatorTimer = (double) getTickCount();
}


void SendDescriptionsToUnity( sDataDescriptions * dataDescriptions );

int _tmain(int argc, _TCHAR* argv[])
{
    int iResult;
    int iConnectionType = ConnectionType_Multicast;
    
    printf("[UnityClient] Motive->Unity3D Relaying Sample Application\n");
    // parse command line args
    if(argc>1)
    {
        strcpy(szServerIPAddress, argv[1]);	// specified on command line
        printf("[UnityClient] Connecting to server at %s\n", szServerIPAddress);
    }
    else
    {
        strcpy(szServerIPAddress, "");		// not specified - assume server is local machine
        printf("[UnityClient] Connecting to server at LocalMachine\n");
    }
    if(argc>3)
    {
        strcpy(szUnityIPAddress, argv[3]);	    // specified on command line
        printf("[UnityClient] Connecting to Unity3D at %s\n", szUnityIPAddress);
    }
    else
    {
        strcpy(szUnityIPAddress, "127.0.0.1");          // not specified - assume server is local machine
        printf("[UnityClient] Connecting to Unity3D on LocalMachine\n");
    }
    if( argc>2 )
    {
        strcpy( szMyIPAddress, argv[ 2 ] );	    // specified on command line
        printf( "[UnityClient] Connecting from %s\n", szMyIPAddress );
    }
    else
    {
        strcpy( szMyIPAddress, "" );          // not specified - assume server is local machine
        printf( "[UnityClient] Connecting from LocalMachine\n" );
    }

    // Create SlipStream
    gSlipStream = new cSlipStream(szUnityIPAddress,16000);

    // Create NatNet Client
    iResult = CreateClient(iConnectionType);

    if(iResult != ErrorCode_OK)
    {
        printf("Error initializing client.  See log for details.  Exiting");
        return 1;
    }
    
	// Retrieve Data Descriptions from server
	printf("[UnityClient] Requesting assets from Motive\n");
	sDataDescriptions* pDataDefs = NULL;
	int nBodies = theClient->GetDataDescriptions(&pDataDefs);
	if(!pDataDefs)
	{
		printf("[UnityClient] Unable to retrieve avatars\n");
	}
	else
	{
        int skeletonCount = 0;
        int rigidBodyCount = 0;

        for(int i=0; i < pDataDefs->nDataDescriptions; i++)
        {
            if(pDataDefs->arrDataDescriptions[i].type == Descriptor_Skeleton)
            {
                // Skeleton
                skeletonCount++;
                sSkeletonDescription* pSK = pDataDefs->arrDataDescriptions[i].Data.SkeletonDescription;
                printf("[UnityClient] Received skeleton description: %s\n", pSK->szName);
                for(int j=0; j < pSK->nRigidBodies; j++)
                {
                    sRigidBodyDescription* pRB = &pSK->RigidBodies[j];
                    // populate bone name dictionary for use in xml ==--
                    gBoneNames[pRB->ID+pSK->skeletonID*100] = pRB->szName;
                }
            }
            if( pDataDefs->arrDataDescriptions[ i ].type == Descriptor_RigidBody )
            {
                rigidBodyCount++;
                sRigidBodyDescription* pRB = pDataDefs->arrDataDescriptions[ i ].Data.RigidBodyDescription;
                printf( "[UnityClient] Received rigid body description: %s\n", pRB->szName );

            }
        } 

        //printf( "[UnityClient] Received %d Skeleton Description(s)\n", skeletonCount );
        //printf( "[UnityClient] Received %d Rigid Body Description(s)\n", rigidBodyCount );

        SendDescriptionsToUnity( pDataDefs );
	}

	// Ready to receive marker stream!
	printf("[UnityClient] Connected to server and ready to relay data to Unity3D\n");
    printf("[UnityClient] Listening for first frame of data...\n");
    gIndicator = Listening;

	int c;
	bool bExit = false;
	while(!bExit)
	{
        while( !kbhit() )
        {
            Sleep( 10 );
        
            if( gIndicator==Streaming )
            {
                if( elapsedTimer()>1.0 )
                {
                    printf("[UnityClient] Data stream stalled.  Listening for more frame data...\n");
                    gIndicator = Listening;
                }
            }
        }
		
        c =_getch();

        switch(c)
		{
			case 'q':
				bExit = true;		
				break;	
			case 'r':
				resetClient();
				break;	
            case 'p':
                sServerDescription ServerDescription;
                memset(&ServerDescription, 0, sizeof(ServerDescription));
                theClient->GetServerDescription(&ServerDescription);
                if(!ServerDescription.HostPresent)
                {
                    printf("Unable to connect to server. Host not present. Exiting.");
                    return 1;
                }
                break;	
            case 'f':
                {
                    sFrameOfMocapData* pData = theClient->GetLastFrameOfData();
                    printf("Most Recent Frame: %d", pData->iFrame);
                }
                break;	
            case 'm':	                        // change to multicast
                iResult = CreateClient(ConnectionType_Multicast);
                if(iResult == ErrorCode_OK)
                    printf("Client connection type changed to Multicast.\n\n");
                else
                    printf("Error changing client connection type to Multicast.\n\n");
                break;
            case 'u':	                        // change to unicast
                iResult = CreateClient(ConnectionType_Unicast);
                if(iResult == ErrorCode_OK)
                    printf("Client connection type changed to Unicast.\n\n");
                else
                    printf("Error changing client connection type to Unicast.\n\n");
                break;


			default:
				break;
		}
		if(bExit)
			break;
	}

	// Done - clean up.
	theClient->Uninitialize();

	return ErrorCode_OK;
}

// Establish a NatNet Client connection
int CreateClient(int iConnectionType)
{
    // release previous server
    if(theClient)
    {
        theClient->Uninitialize();
        delete theClient;
    }

    // create NatNet client
    theClient = new NatNetClient(iConnectionType);

    theClient->SetVerbosityLevel( Verbosity_None );

    // [optional] use old multicast group
    //theClient->SetMulticastAddress("224.0.0.1");

    // print version info
    unsigned char ver[4];
    theClient->NatNetVersion(ver);
   
    // Set callback handlers
    theClient->SetMessageCallback(MessageHandler);
    theClient->SetDataCallback( DataHandler, theClient );	// this function will receive data from the server

    // Init Client and connect to NatNet server
    // to use NatNet default port assigments
    int retCode = theClient->Initialize(szMyIPAddress, szServerIPAddress);
    // to use a different port for commands and/or data:
    //int retCode = theClient->Initialize(szMyIPAddress, szServerIPAddress, MyServersCommandPort, MyServersDataPort);
    if (retCode != ErrorCode_OK)
    {
        printf("Unable to connect to server.  Error code: %d. Exiting", retCode);
        return ErrorCode_Internal;
    }
    else
    {
        // print server info
        sServerDescription ServerDescription;
        memset(&ServerDescription, 0, sizeof(ServerDescription));
        theClient->GetServerDescription(&ServerDescription);
        if(!ServerDescription.HostPresent)
        {
            printf("Unable to connect to server. Host not present. Exiting.");
            return 1;
        }
        printf( "[UnityClient] Server    : %s\n", ServerDescription.szHostComputerName );
        printf( "[UnityClient] Server  IP: %s\n", szServerIPAddress );
        printf( "[UnityClient] Unity3D IP: %s\n", szUnityIPAddress );
        printf( "[UnityClient] Current IP: %s\n", szMyIPAddress );
    }

    return ErrorCode_OK;

}

void SendDescriptionsToUnity( sDataDescriptions * dataDescriptions )
{
    //== lets send the skeleton descriptions to Unity ==--

    std::vector<int> skeletonDescriptions;

    for( int index=0; index<dataDescriptions->nDataDescriptions; index++ )
    {
        if( dataDescriptions->arrDataDescriptions[ index ].type==Descriptor_Skeleton )
        {
            skeletonDescriptions.push_back( index );
        }
    }

    //== early out if no skeleton descriptions to stream ==--

    if( skeletonDescriptions.size()==0 )
    {
        return;
    }

    //== now we stream skeleton descriptions over XML ==--

    std::ostringstream xml;

    xml << "<?xml version=\"1.0\" ?>" << std::endl;
    xml << "<Stream>" << std::endl;
    xml << "<SkeletonDescriptions>" << std::endl;

    // skeletons first

    for( int descriptions=0; descriptions<(int) skeletonDescriptions.size(); descriptions++ )
    {
        int index = skeletonDescriptions[ descriptions ];

        // Skeleton
        sSkeletonDescription* pSK = dataDescriptions->arrDataDescriptions[ index ].Data.SkeletonDescription;

        xml << "<SkeletonDescription ID=\"" << pSK->skeletonID << "\" ";
        xml << "Name=\"" << pSK->szName << "\" ";
        xml << "BoneCount=\"" << pSK->nRigidBodies << "\">" << std::endl;

        for( int j=0; j < pSK->nRigidBodies; j++ )
        {
            sRigidBodyDescription* pRB = &pSK->RigidBodies[ j ];

            xml << "<BoneDefs ID=\"" << pRB->ID << "\" ";

            xml << "ParentID=\"" << pRB->parentID << "\" ";
            xml << "Name=\"" << pRB->szName << "\" ";
            xml << "x=\"" << pRB->offsetx << "\" ";
            xml << "y=\"" << pRB->offsety << "\" ";
            xml << "z=\"" << pRB->offsetz << "\"/>" << std::endl;
        }

        xml << "</SkeletonDescription>" << std::endl;
    }

    xml << "</SkeletonDescriptions>" << std::endl;
    xml << "</Stream>" << std::endl;


    // convert xml document into a buffer filled with data ==--

    std::string str =  xml.str();
    const char* buffer = str.c_str();

    // stream xml data over UDP via SlipStream ==--

    gSlipStream->Stream( (unsigned char *) buffer, (int) strlen( buffer ) );
}



// Create XML from frame data and output to Unity
void SendFrameToUnity( sFrameOfMocapData *data, void *pUserData )
{
    if( data->Skeletons>0 )
    {
        std::ostringstream xml;

        xml << "<?xml version=\"1.0\" ?>" << std::endl;
        xml << "<Stream>" << std::endl;
        xml << "<Skeletons>" << std::endl;

        for( int i=0; i<data->nSkeletons; i++ )
        {
            sSkeletonData skData = data->Skeletons[ i ]; // first skeleton ==--

            xml << "<Skeleton ID=\"" << skData.skeletonID << "\">" << std::endl;

            for( int i=0; i<skData.nRigidBodies; i++ )
            {
                sRigidBodyData rbData = skData.RigidBodyData[ i ];

                xml << "<Bone ID=\"" << rbData.ID << "\" ";
                xml << "Name=\"" << gBoneNames[ LOWORD( rbData.ID )+skData.skeletonID*100 ].c_str() << "\" ";
                xml << "x=\"" << rbData.x << "\" ";
                xml << "y=\"" << rbData.y << "\" ";
                xml << "z=\"" << rbData.z << "\" ";
                xml << "qx=\"" << rbData.qx << "\" ";
                xml << "qy=\"" << rbData.qy << "\" ";
                xml << "qz=\"" << rbData.qz << "\" ";
                xml << "qw=\"" << rbData.qw << "\" />" << std::endl;
            }

            xml << "</Skeleton>" << std::endl;
        }

        xml << "</Skeletons>" << std::endl;
        xml << "<RigidBodies>" << std::endl;

        // rigid bodies ==--

        for( int i=0; i<data->nRigidBodies; i++ )
        {
            sRigidBodyData rbData = data->RigidBodies[ i ];

            xml << "<RigidBody ID=\"" << rbData.ID << "\" ";
            xml << "x=\"" << rbData.x << "\" ";
            xml << "y=\"" << rbData.y << "\" ";
            xml << "z=\"" << rbData.z << "\" ";
            xml << "qx=\"" << rbData.qx << "\" ";
            xml << "qy=\"" << rbData.qy << "\" ";
            xml << "qz=\"" << rbData.qz << "\" ";
            xml << "qw=\"" << rbData.qw << "\" />" << std::endl;
        }


        xml << "</RigidBodies>" << std::endl;
        xml << "</Stream>" << std::endl;

        std::string str =  xml.str();
        const char* buffer = str.c_str();

        // stream xml data over UDP via SlipStream ==--

        gSlipStream->Stream( (unsigned char *) buffer, (int) strlen( buffer ) );
    }
}


double gLastDescription=0;

// DataHandler receives data from the server
void __cdecl DataHandler( sFrameOfMocapData* data, void* pUserData )
{
    NatNetClient* pClient = (NatNetClient*) pUserData;

    catchUp();

    if( gIndicator==Listening )
    {
        gIndicator=Streaming;
        printf("[UnityClient] Receiving data and streaming to Unity3D\n");
    }

    SendFrameToUnity( data, pUserData );

    double timer = data->fTimestamp;

    if( timer-gLastDescription>1 || gLastDescription>timer )
    {
        //== stream skeleton definitions once per second ==--
        gLastDescription = timer;
        sDataDescriptions* pDataDefs = NULL;
        int nBodies = theClient->GetDataDescriptions( &pDataDefs );
        if( pDataDefs )
        {
            for( int i=0; i < pDataDefs->nDataDescriptions; i++ )
            {
                if( pDataDefs->arrDataDescriptions[ i ].type == Descriptor_Skeleton )
                {
                    // Skeleton
                    sSkeletonDescription* pSK = pDataDefs->arrDataDescriptions[ i ].Data.SkeletonDescription;
                    for( int j=0; j < pSK->nRigidBodies; j++ )
                    {
                        sRigidBodyDescription* pRB = &pSK->RigidBodies[ j ];
                        // populate bone name dictionary for use in xml ==--
                        gBoneNames[ pRB->ID+pSK->skeletonID*100 ] = pRB->szName;
                    }
                }
            }
            SendDescriptionsToUnity( pDataDefs );
        }
    }
}

// MessageHandler receives NatNet error/debug messages
void __cdecl MessageHandler(int msgType, char* msg)
{
	printf("\n%s\n", msg);
}

void resetClient()
{
	int iSuccess;

	printf("\n\nre-setting Client\n\n.");

	iSuccess = theClient->Uninitialize();
	if(iSuccess != 0)
		printf("error un-initting Client\n");

	iSuccess = theClient->Initialize(szMyIPAddress, szServerIPAddress);
	if(iSuccess != 0)
		printf("error re-initting Client\n");


}

