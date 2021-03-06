namespace Bender.Nodes.FormUrlEncoded
{
    public class FormValueNode : NodeBase
    {
        private string _name;
        private string _value;

        public FormValueNode(string name, string value = null)
        {
            _name = name;
            _value = value;
        }

        public override string Type => "form value";
        public override string Format => "url encoded";
        public override bool IsNamed => true;
        public override string Path => _name;

        protected override NodeType GetNodeType()
        {
            return NodeType.Value;
        }

        protected override void SetNodeType(NodeType nodeType)
        {
            if (nodeType != NodeType.Value)
                throw new BenderException("Form values must be value nodes.");
        }

        protected override string GetName()
        {
            return _name;
        }

        protected override void SetName(string name)
        {
            _name = name;
        }

        protected override object GetValue()
        {
            return _value;
        }

        protected override void SetValue(object value)
        {
            _value = value == null ? "" : (value is bool ?
                value.ToString().ToLower() : value.ToString());
        }
    }
}