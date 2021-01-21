using System;

namespace Matrix.Domain.Structure
{
    public class ReferenceAttribute : Attribute
    {
        public Type ReferenceType { get; private set; }
        public BorrowAclType BorrowAclType { get; private set; }
        public string PropertyName { get; private set; }

        public ReferenceAttribute(Type referenceType, BorrowAclType borrowAclType = BorrowAclType.NoBorrow)
        {
            ReferenceType = referenceType;
            BorrowAclType = borrowAclType;
        }
        public ReferenceAttribute(Type referenceType, BorrowAclType borrowAclType, string propertyName)
        {
            ReferenceType = referenceType;
            BorrowAclType = borrowAclType;
            PropertyName = propertyName;
        }
    }
}
