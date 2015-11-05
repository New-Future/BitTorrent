using System;
using System.Threading;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    /// <summary>
    /// 下载模式
    /// </summary>
    public class DownloadMode:TorrentMode
    {
        private const int RequestsQueueLength = 10;
        private long pendingWrites = 0;
        private const int MaxConnectedPeers = 100;

        public DownloadMode(HashingMode hashMode):
            this(new BlockManager(hashMode.Metadata, hashMode.BlockManager.MainDirectory), hashMode.BlockStrategist, hashMode.Metadata, hashMode.Monitor)
        { 
        }

        public DownloadMode(BlockManager manager, BlockStrategist strategist, TorrentData metadata, TransferMonitor monitor) :
            base(manager, strategist, metadata, monitor)
        {
            strategist.HavePiece += (sender, args) => SendHaveMessages(args.Value);
        }

        public override void Start()
        {
            base.Start();
            if(BlockStrategist.Complete)
            {
                OnDownloadComplete();
                OnFlushedToDisk();
                Stop(true);
                return;
            }
            
            PeerListener.Register(Metadata.InfoHash, peer => SendHandshake(peer, DefaultHandshake));
        }

        /// <summary>
        /// 暂停
        /// </summary>
        /// <param name="closeStreams"></param>
        public override void Stop(bool closeStreams)
        {
            base.Stop(closeStreams);
            PeerListener.Deregister(Metadata.InfoHash);
        }

        /// <summary>
        /// 发生请求
        /// </summary>
        /// <param name="request"></param>
        /// <param name="peer"></param>
        protected override void HandleRequest(RequestMessage request, PeerState peer)
        {
            if(!peer.IsChoked && request.Length <= Global.Instance.BlockSize)
            {
                BlockManager.GetBlock(new byte[request.Length], request.Index, request.Offset, request.Length, BlockRead, peer);
            }
        }

        /// <summary>
        /// 收到有用块
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="peer"></param>
        protected override void HandlePiece(PieceMessage piece, PeerState peer)
        {
            var blockInfo = new BlockInfo(piece.Index, piece.Offset, piece.Data.Length);
            if(BlockStrategist.Received(blockInfo))
            {  //写入块
                WriteBlock(piece);
            }
            //block-1
            peer.PendingBlocks--;
            //继续请求Block
            SendBlockRequests(peer);
        }
        
        /// <summary>
        /// 尝试有效接受
        /// </summary>
        /// <param name="unchoke"></param>
        /// <param name="peer"></param>
        protected override void HandleUnchoke(UnchokeMessage unchoke, PeerState peer)
        {
            base.HandleUnchoke(unchoke, peer);
            SendBlockRequests(peer);
        }

        /// <summary>
        /// 和其他peer首次通信
        /// </summary>
        /// <param name="bitfield"></param>
        /// <param name="peer"></param>
        protected override void HandleBitfield(BitfieldMessage bitfield, PeerState peer)
        {   //首次触发 TorrentMode
            base.HandleBitfield(bitfield, peer);
            if(!peer.NoBlocks)
            {   //如果有可用块则发生Intested的消息
                SendMessage(peer, new InterestedMessage());
            }
        }

        /// <summary>
        /// 收到interested的数据
        /// </summary>
        /// <param name="interested"></param>
        /// <param name="peer"></param>
        protected override void HandleInterested(InterestedMessage interested, PeerState peer)
        {
            base.HandleInterested(interested, peer);
            peer.IsChoked = false;
            SendMessage(peer, new UnchokeMessage());
        }

        protected override bool AddPeer(PeerState peer)
        {
            if (Peers.Count >= MaxConnectedPeers) return false;

            SendBitfield(peer);
            return base.AddPeer(peer);
        }

        private void SendBitfield(PeerState peer)
        {
            SendMessage(peer, new BitfieldMessage(BlockStrategist.Bitfield));
        }

        /// <summary>
        /// 发送块请求
        /// </summary>
        /// <param name="peer"></param>
        private void SendBlockRequests(PeerState peer)
        {   //计算所需的块数
            int count = RequestsQueueLength - peer.PendingBlocks;
            for(int i=0;i<count;i++)
            {   //请求新的块
                var block = BlockStrategist.Next(peer.Bitfield);
                if (block != BlockInfo.Empty) 
                {   //块有效发生请求，计数+1
                    SendMessage(peer, new RequestMessage(block.Index, block.Offset, block.Length));
                    peer.PendingBlocks++;
                }
                else if (BlockStrategist.Complete)
                {   //地址无效而且下载完成
                    OnDownloadComplete();
                    return;
                }
            }
        }

        private void SendHaveMessages(int piece)
        {
            foreach(var peer in Peers.Values)
            {
                if(!peer.Bitfield[piece])
                    SendMessage(peer, new HaveMessage(piece));
            }
        }

        /// <summary>
        /// 写入块
        /// </summary>
        /// <param name="piece"></param>
        private void WriteBlock(PieceMessage piece)
        {
            try
            {
                var block = new Block(piece.Data, piece.Index, piece.Offset, piece.Data.Length);
                BlockManager.AddBlock(block, BlockWritten, block);
                Interlocked.Add(ref pendingWrites, piece.Data.Length);
            }
            catch(Exception e)
            {
                HandleException(e);
            }
        }

        private void BlockWritten(bool success, object state)
        {
            var block = (Block)state;
            if(success)
            {
                Monitor.Written(block.Info.Length);
                Interlocked.Add(ref pendingWrites, -block.Info.Length);
            }
            if (BlockStrategist.Complete && pendingWrites==0)
                AllWrittenToDisk();
        }

        private void BlockRead(bool success, Block block, object state)
        {
            var peer = (PeerState)state;
            try
            {
                if (success)
                {
                    Monitor.Read(block.Info.Length);
                    SendMessage(peer, new PieceMessage(block.Info.Index, block.Info.Offset, block.Data));
                }
            }
            catch(Exception e)
            {
                HandleException(e);
            }
        }

        private void AllWrittenToDisk()
        {
            Stop(true);
            OnFlushedToDisk();
        }

        private void OnDownloadComplete()
        {
            EventHandler handler = DownloadComplete;
            if(handler != null) handler(this, new EventArgs());
        }

        private void OnFlushedToDisk()
        {
            EventHandler handler = FlushedToDisk;
            if(handler != null) handler(this, new EventArgs());
        }

        public event EventHandler DownloadComplete;
        public event EventHandler FlushedToDisk;
    }
}
