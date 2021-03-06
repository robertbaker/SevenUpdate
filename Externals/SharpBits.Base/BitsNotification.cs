// <copyright file="BitsNotification.cs" project="SharpBits.Base">Xidar</copyright>
// <license href="http://sharpbits.codeplex.com/license" name="BSD License" />

namespace SharpBits.Base
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>The notification class for the bits manager.</summary>
    internal class BitsNotification : IBackgroundCopyCallback
    {
        /// <summary>The BITS manager.</summary>
        readonly BitsManager manager;

        /// <summary>Occurs when a <c>BitsJob</c> error occurs.</summary>
        EventHandler<ErrorNotificationEventArgs> errorOccurred;

        /// <summary>Occurs when a <c>BitsJob</c> is modified.</summary>
        EventHandler<NotificationEventArgs> onJobModified;

        /// <summary>Occurs when a <c>BitsJob</c> is transfered.</summary>
        EventHandler<NotificationEventArgs> onJobTransfered;

        /// <summary>Initializes a new instance of the <see cref="BitsNotification" /> class.</summary>
        /// <param name="manager">The manager.</param>
        internal BitsNotification(BitsManager manager)
        {
            this.manager = manager;
        }

        /// <summary>Occurs when [on job error event].</summary>
        public event EventHandler<ErrorNotificationEventArgs> OnJobErrorEvent
        {
            add { this.errorOccurred += value; }

            remove { this.errorOccurred -= value; }
        }

        /// <summary>Occurs when [on job modified event].</summary>
        public event EventHandler<NotificationEventArgs> OnJobModifiedEvent
        {
            add { this.onJobModified += value; }

            remove { this.onJobModified -= value; }
        }

        /// <summary>Occurs when [on job transferred event].</summary>
        public event EventHandler<NotificationEventArgs> OnJobTransferredEvent
        {
            add { this.onJobTransfered += value; }

            remove { this.onJobTransfered -= value; }
        }

        /// <summary>Called when an error occurs.</summary>
        /// <param name="copyJob">Contains job-related information, such as the number of bytes and files transferred before the error occurred. It also contains the methods to resume and cancel the job. Do not release pJob; BITS releases the interface when the JobError method returns.</param>
        /// <param name="error">Contains error information, such as the file being processed at the time the fatal error occurred and a description of the error. Do not release pError; BITS releases the interface when the JobError method returns.</param>
        public void JobError(IBackgroundCopyJob copyJob, IBackgroundCopyError error)
        {
            if (this.manager == null)
            {
                return;
            }

            BitsJob job;
            if (null == this.errorOccurred)
            {
                return;
            }

            Guid guid;
            copyJob.GetId(out guid);
            if (this.manager.Jobs.ContainsKey(guid))
            {
                job = this.manager.Jobs[guid];
            }
            else
            {
                // Update Job list to check whether the job still exists. If not, just return
                this.manager.EnumJobs(this.manager.CurrentOwner);
                if (this.manager.Jobs.ContainsKey(guid))
                {
                    job = this.manager.Jobs[guid];
                }
                else
                {
                    return;
                }
            }

            this.errorOccurred(this, new ErrorNotificationEventArgs(job, new BitsError(job, error)));

            // forward event
            if (job.NotificationTarget == null)
            {
            }
            else
            {
                try
                {
                    job.NotificationTarget.JobError(copyJob, error);
                }
                catch (COMException)
                {
                }
            }
        }

        /// <summary>Called when a job is modified.</summary>
        /// <param name="copyJob">Contains the methods for accessing property, progress, and state information of the job. Do not release pJob; BITS releases the interface when the JobModification method returns.</param>
        /// <param name="reserved">Reserved for future use.</param>
        public void JobModification(IBackgroundCopyJob copyJob, uint reserved)
        {
            if (this.manager == null)
            {
                return;
            }

            BitsJob job;
            if (null == this.onJobModified)
            {
                return;
            }

            Guid guid;
            copyJob.GetId(out guid);
            if (this.manager.Jobs.ContainsKey(guid))
            {
                job = this.manager.Jobs[guid];
            }
            else
            {
                // Update Job list to check whether the job still exists. If not, just return
                this.manager.EnumJobs(this.manager.CurrentOwner);
                if (this.manager.Jobs.ContainsKey(guid))
                {
                    job = this.manager.Jobs[guid];
                }
                else
                {
                    return;
                }
            }

            this.onJobModified(this, new NotificationEventArgs(job));

            // forward event
            if (job.NotificationTarget == null)
            {
            }
            else
            {
                try
                {
                    job.NotificationTarget.JobModification(copyJob, reserved);
                }
                catch (COMException)
                {
                }
            }
        }

        /// <summary>Called when all of the files in the job have successfully transferred.</summary>
        /// <param name="copyJob">Contains job-related information, such as the time the job completed, the number of bytes transferred, and the number of files transferred. Do not release pJob; BITS releases the interface when the method returns.</param>
        public void JobTransferred(IBackgroundCopyJob copyJob)
        {
            if (this.manager == null)
            {
                return;
            }

            BitsJob job;
            if (null == this.onJobTransfered)
            {
                return;
            }

            Guid guid;
            copyJob.GetId(out guid);
            if (this.manager.Jobs.ContainsKey(guid))
            {
                job = this.manager.Jobs[guid];
            }
            else
            {
                // Update Job list to check whether the job still exists. If not, just return
                this.manager.EnumJobs(this.manager.CurrentOwner);
                if (this.manager.Jobs.ContainsKey(guid))
                {
                    job = this.manager.Jobs[guid];
                }
                else
                {
                    return;
                }
            }

            this.onJobTransfered(this, new NotificationEventArgs(job));

            // forward event
            if (job.NotificationTarget == null)
            {
            }
            else
            {
                try
                {
                    job.NotificationTarget.JobTransferred(copyJob);
                }
                catch (COMException)
                {
                }
            }
        }
    }
}