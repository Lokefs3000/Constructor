using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Primary.Input.Bindings;

namespace Primary.Input
{
    public sealed class InputScheme
    {
        private string _name;

        private List<InputAction> _actions;

        public InputScheme()
        {
            _name = string.Empty;

            _actions = new List<InputAction>();
        }

        public void UpdateActions()
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                _actions[i].UpdateValues();
            }
        }

        public InputAction? FindAction(ReadOnlySpan<char> path)
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                if (path.Equals(_actions[i].Name, StringComparison.Ordinal))
                {
                    return _actions[i];
                }
            }

            return null;
        }

        public IInputBinding? FindBinding(string path)
        {
            ReadOnlySpanTokenizer<char> tokenizer = path.Tokenize('/');
            if (tokenizer.MoveNext())
            {
                InputAction? action = FindAction(tokenizer.Current);
                if (action != null)
                {
                    IInputBinding? binding = null;
                    foreach (ReadOnlySpan<char> name in tokenizer)
                    {
                        binding = binding?.FindBinding(name);
                        if (binding == null)
                            return null;
                    }

                    return binding;
                }
            }

            return null;
        }

        public InputAction AddAction()
        {
            InputAction action = new InputAction();
            _actions.Add(action);
            return action;
        }

        public void RemoveAction(InputAction action) => _actions.Remove(action);

        public string Name { get => _name; set => _name = value; }

        public IReadOnlyList<InputAction> Actions => _actions;
    }
}
