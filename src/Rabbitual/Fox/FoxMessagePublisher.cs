﻿using System;

namespace Rabbitual.Fox
{
    public class FoxMessagePublisher: IPublisher
    {
        private readonly Hub _hub;

        public FoxMessagePublisher(Hub hub)
        {
            _hub = hub;
        }

        public void EnqueueTask(Message task)
        {
            _hub.EnqueueTask(task);
        }

        public void PublishEvent(Message e)
        {
            _hub.PublishEvent(e);
        }
    }
}