using System;

namespace LibDerailer.CCodeGen
{
    public class TypeName
    {
        public bool IsPointer { get; }

        /* Not sure how to do this best in C#.
         * In C++ I'd use a std::optional<std::string, std::unique_ptr<TypeName>> or something.
         */
        public string   Name    { get; }
        public TypeName SubType { get; }

        public TypeName()
        {
        }

        public TypeName(string name, bool isPointer)
        {
            Name      = name;
            IsPointer = isPointer;
        }

        public TypeName(TypeName subType, bool isPointer)
        {
            SubType   = subType;
            IsPointer = isPointer;
        }

        public override string ToString()
        {
            if (Name == null && SubType == null)
                return IsPointer ? "void*" : "void";
            if (Name != null && SubType != null)
                throw new InvalidOperationException("Only one option may be set");

            string enclosed = Name ?? SubType.ToString();
            if (IsPointer)
                enclosed += "*";
            return enclosed;
        }
    }
}