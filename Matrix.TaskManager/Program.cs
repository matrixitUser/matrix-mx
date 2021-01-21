﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Matrix.TaskManager
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(host =>
            {
                host.Service<Service>(service =>
                {
                    service.ConstructUsing(() => new Service());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });
                host.RunAsLocalSystem();
                host.SetDisplayName("Матрикс.ДиспетчерЗадач");
                host.SetDescription("Управляет маршрутами опроса, содержит очереди задач");
                host.SetServiceName("Matrix.TaskManager");
            });
        }
    }
}