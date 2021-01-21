using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Common.Infrastructure.Protocol.Messages;
using System.Timers;

namespace Matrix.Common.Infrastructure.Protocol
{
    public class Couple<TMessage, TElement> where TMessage : DoMessage
    {
        public int Timeout { get; set; }
        public int MaxCount { get; set; }

        private Timer sendTimer = new Timer();

        private List<TElement> elements = new List<TElement>();

        private Func<IEnumerable<TElement>, TMessage> messageFactory;

        public Couple(Func<IEnumerable<TElement>, TMessage> messageFactory, int timeout, int maxCount)
        {
            this.messageFactory = messageFactory;
            Timeout = timeout;
            MaxCount = maxCount;
            sendTimer.Interval = Timeout;
            sendTimer.AutoReset = false;
            sendTimer.Elapsed += (se, ea) =>
            {
                lock (elements)
                {
                    if (elements.Count > 0)
                    {
                        RaiseCoupleMessageReady(elements);
                        elements.Clear();
                    }
                }
            };
        }

        public Couple(Func<IEnumerable<TElement>, TMessage> messageFactory)
            : this(messageFactory, 60000, 100)
        {

        }

        public void Add(TElement e)
        {
            Add(new TElement[] { e });
        }

        public void Add(IEnumerable<TElement> e)
        {
            lock (elements)
            {
                var isFirstTime = elements.Count == 0;
                elements.AddRange(e);

                if (elements.Count >= MaxCount)
                {
                    RaiseCoupleMessageReady(elements);
                    elements.Clear();
                    return;
                }
                if (isFirstTime)
                {
                    sendTimer.Start();
                    return;
                }
            }
        }

        public event EventHandler<CoupleMessageReadyEventArgs<TMessage>> OnCoupleMessageReady;

        private void RaiseCoupleMessageReady(IEnumerable<TElement> els)
        {
            var message = messageFactory(els);
            sendTimer.Stop();
            if (OnCoupleMessageReady != null)
            {
                OnCoupleMessageReady(this, new CoupleMessageReadyEventArgs<TMessage>(message));
            }
        }
    }

    public class CoupleMessageReadyEventArgs<TMessage> : EventArgs where TMessage : DoMessage
    {
        public TMessage Message { get; private set; }
        public CoupleMessageReadyEventArgs(TMessage message)
        {
            Message = message;
        }
    }
}
