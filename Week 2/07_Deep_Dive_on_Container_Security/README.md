# Deep Dive ON Container Security

Course by [Bertram Dorn - Amazon Web Services (AWS)](https://www.linkedin.com/in/bertram-dorn-46aa71/)

There are 3 types of risks in Containers

1. Confidentiality(Segregation)

   - Container to container
   - Process to Process
   - Container to Outside

2. Integrity(Access)

   - Who / When / Where
   - Logging
   - Start/Stop
   - Content

3. Availability (Resource Usage)

## Namespaces

Needs clone(2) to start into a Namespace

1. PID/Process-Namespace
   - Each process has a global and local PID
   - processes of different Namespaces- Trees can't see each other
   - 1st process in a new Namespace has PID1, others are linked to it
   - Modifying a process is possible in same NS or Child
   - Still all on the same Memory Management
   - Use clone(2) to get in
   - Fork(2) is available inside
2. CPU/Memory-Namespace (cgroup)

- Policy based scheduling
- CPU Affinity possible, but not always enforced
- Memory Limitation is possible (Out of Memory, or "oom" killer)
- Dirty/used/empty pages is a global topic
- Be aware of Context switches
- Be aware of CPU Threads
- Memory Management quite difficult and might have challanges

3. Network-Namespace
   - Puts interfaces into Namespaces
   - Routing/Forwarding/Filters/Bridging happens in kernel
   - All processes in a net-namespace can talk to the interface
   - TCP/UDP/ICMP stacks are still in the kernel
   - See corresponding Syscalls
   - ENI => Instance => [Pause(net namespace) <=> App], Here apps in same namespace via `Pause` could communicate with each other (**This is how AWSVPC in IP works**)
4. User-Namespace (UID Namespace)
   - Mapping table for Users for local and global context
   - User management in NS possible, but not recommended
5. FS/Mount-Namespace
   - Mapping table for Paths
   - See Syscall for open
6. IPC-Namespace

   - Communication between two containers

7. UTS Namespace (Give container a possibility to have its own host)

## Keep in Mind

1. Linux containers, as of today, sit on a shared Kernel
2. They sit on a shared Platform
3. They can influence each other quite easy
4. Even if process-to-process isolation is tight, it is just one layer
5. Networking is always a discussion

### General Recommendation

Make a risk assesment of which workloads to put on your platform together

### General Considerations for Containers

- Keep them small and start from scratch
- No Login from the outside world
  1. No install of openssh in the container
  2. Exec your shell into the container (Change NS)
- Outside communication is critical
- Container Technology != Network Encryption
  1. Please encrypt your network traffic by using TLS
  2. Maintain end to end communication encryption
