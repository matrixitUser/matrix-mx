﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.CheckServer.Handlers
{
    class HandlerManager
    {
        private readonly List<IHandler> handlers = new List<IHandler>();
        public IHandler Get(string what)
        {
            foreach (var handler in handlers)
            {
                if (handler.CanHandle(what)) return handler;
            }
            return null;
        }

        public void Register(IHandler handler)
        {
            handlers.Add(handler);
        }

        private HandlerManager()
        {

        }
        private static HandlerManager instance = null;
        public static HandlerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HandlerManager();
                }
                return instance;
            }
        }
    }
}
