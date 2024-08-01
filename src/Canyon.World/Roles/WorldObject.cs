namespace Canyon.World.Roles
{
    public abstract class WorldObject
    {
        public virtual uint Identity { get; init; }
        public virtual string Name { get; protected set; }

        public virtual ushort X { get; set; }
        public virtual ushort Y { get; set; }

        /// <summary>
        /// Defines if this role activates the chunk for processing.
        /// </summary>
        public virtual bool IsProcessable => false;
    }
}