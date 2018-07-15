#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
namespace CSharpTest.Net.RpcLibrary
{
#pragma warning disable 1591
    /// <summary>
    /// Defines the various types of protocols that are supported by Win32 RPC
    /// </summary>
    public enum RpcProtseq
    {
        /// <summary>
        /// Connection-oriented NetBIOS over Transmission Control Protocol (TCP) Client only: MS-DOS, Windows 3.x
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT
        /// </summary>
        ncacn_nb_tcp,
        /// <summary>
        /// Connection-oriented NetBIOS over Internet Packet Exchange (IPX) Client only: MS-DOS, Windows 3.x
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT
        /// </summary>
        ncacn_nb_ipx,
        /// <summary>
        /// Connection-oriented NetBIOS Enhanced User Interface (NetBEUI) Client only: MS-DOS, Windows 3.x
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT, Windows Me, Windows 98, Windows 95
        /// </summary>
        ncacn_nb_nb,
        /// <summary>
        /// Connection-oriented Transmission Control Protocol/Internet Protocol (TCP/IP) Client only: MS-DOS, Windows 3.x, and Apple Macintosh
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT, Windows Me, Windows 98, Windows 95
        /// </summary>
        ncacn_ip_tcp,
        /// <summary>
        /// Connection-oriented named pipes Client only: MS-DOS, Windows 3.x, Windows 95
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT
        /// </summary>
        ncacn_np,
        /// <summary>
        /// Connection-oriented Sequenced Packet Exchange (SPX) Client only: MS-DOS, Windows 3.x
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT, Windows Me, Windows 98, Windows 95
        /// </summary>
        ncacn_spx,
        /// <summary>
        /// Connection-oriented DECnet transport 
        /// Client only: MS-DOS, Windows 3.x
        /// </summary>
        ncacn_dnet_nsp,
        /// <summary>
        /// Connection-oriented AppleTalk DSP Client: Apple Macintosh
        /// Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT
        /// </summary>
        ncacn_at_dsp,
        /// <summary>
        /// Connection-oriented Vines scalable parallel processing (SPP) transport Client only: MS-DOS, Windows 3.x
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT
        /// </summary>
        ncacn_vns_spp,
        /// <summary>
        /// Datagram (connectionless) User Datagram Protocol/Internet Protocol (UDP/IP) Client only: MS-DOS, Windows 3.x
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT
        /// </summary>
        ncadg_ip_udp,
        /// <summary>
        /// Datagram (connectionless) IPX Client only: MS-DOS, Windows 3.x
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT
        /// </summary>
        ncadg_ipx,
        /// <summary>
        /// Datagram (connectionless) over the Microsoft Message Queue Server (MSMQ) Client only: Windows Me/98/95
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT Server 4.0 with SP3 and later
        /// </summary>
        ncadg_mq,
        /// <summary>
        /// Connection-oriented TCP/IP using Microsoft Internet Information Server as HTTP proxy Client only: Windows Me/98/95
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000
        /// </summary>
        ncacn_http,
        /// <summary>
        /// Local procedure call 
        /// Client and Server: Windows Server 2003, Windows XP, Windows 2000, Windows NT, Windows Me, Windows 98, Windows 95
        /// </summary>
        ncalrpc
    }
    /// <summary>
    /// Defines the type of protocol the client is connected with
    /// </summary>
    public enum RpcProtoseqType : uint
    {
        /// <summary> TCP, UDP, IPX over TCP, etc </summary>
        TCP = 0x1,
        /// <summary> Named Pipes </summary>
        NMP = (0x2),
        /// <summary> LPRC / Local RPC </summary>
        LRPC = (0x3),
        /// <summary> HTTP / IIS integrated </summary>
        HTTP = (0x4),
    }

    /// <summary> WIN32 RPC Error Codes </summary>
    public enum RpcError : uint
    {
        RPC_S_OK = 0,
        RPC_S_INVALID_ARG = 87,
        RPC_S_OUT_OF_MEMORY = 14,
        RPC_S_OUT_OF_THREADS = 164,
        RPC_S_INVALID_LEVEL = 87,
        RPC_S_BUFFER_TOO_SMALL = 122,
        RPC_S_INVALID_SECURITY_DESC = 1338,
        RPC_S_ACCESS_DENIED = 5,
        RPC_S_SERVER_OUT_OF_MEMORY = 1130,
        RPC_S_ASYNC_CALL_PENDING = 997,
        RPC_S_UNKNOWN_PRINCIPAL = 1332,
        RPC_S_TIMEOUT = 1460,
        RPC_S_ALREADY_REGISTERED = 1711,
        RPC_S_TYPE_ALREADY_REGISTERED = 1712,
        RPC_S_ALREADY_LISTENING = 1713,
        RPC_S_NO_PROTSEQS_REGISTERED = 1714,
        RPC_S_NOT_LISTENING = 1715,
        RPC_S_DUPLICATE_ENDPOINT = 1740,
        RPC_S_BINDING_HAS_NO_AUTH = 1746,
        RPC_S_CANNOT_SUPPORT = 1764,
        RPC_E_FAIL = 0x80004005u
    }

    /// <summary>
    /// The protection level of the communications, RPC_C_PROTECT_LEVEL_PKT_PRIVACY is 
    /// the default for authenticated communications.
    /// </summary>
    public enum RpcProtectionLevel : uint
    {
        RPC_C_PROTECT_LEVEL_DEFAULT = 0,
        RPC_C_PROTECT_LEVEL_NONE = 1,
        RPC_C_PROTECT_LEVEL_CONNECT = 2,
        RPC_C_PROTECT_LEVEL_CALL = 3,
        RPC_C_PROTECT_LEVEL_PKT = 4,
        RPC_C_PROTECT_LEVEL_PKT_INTEGRITY = 5,
        RPC_C_PROTECT_LEVEL_PKT_PRIVACY = 6,
    }

    /// <summary>
    /// The authentication type to be used for connection, GSS_NEGOTIATE / WINNT
    /// are the most common.  Be aware that GSS_NEGOTIATE is not available unless
    /// the machin is a member of a domain that is not running WinNT (or in legacy 
    /// mode).
    /// </summary>
    public enum RpcAuthentication : uint
    {
        RPC_C_AUTHN_NONE = 0,
        RPC_C_AUTHN_DCE_PRIVATE = 1,
        RPC_C_AUTHN_DCE_PUBLIC = 2,
        RPC_C_AUTHN_DEC_PUBLIC = 4,
        RPC_C_AUTHN_GSS_NEGOTIATE = 9,
        RPC_C_AUTHN_WINNT = 10,
        RPC_C_AUTHN_GSS_SCHANNEL = 14,
        RPC_C_AUTHN_GSS_KERBEROS = 16,
        RPC_C_AUTHN_DPA = 17,
        RPC_C_AUTHN_MSN = 18,
        RPC_C_AUTHN_DIGEST = 21,
        RPC_C_AUTHN_MQ = 100,
        RPC_C_AUTHN_DEFAULT = 0xFFFFFFFFu
    }
}