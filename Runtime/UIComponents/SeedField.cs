using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using TextOverflow = Unity.AppUI.UI.TextOverflow;
using Toggle = Unity.AppUI.UI.Toggle;

namespace Unity.Muse.Sprite.UIComponents
{
    /// <summary>
    /// Seed Field UI element.
    /// </summary>
    internal class SeedField : VisualElement, IValidatableElement<int>, ISizeableElement
    {
        /// <summary>
        /// The SeedField main styling class.
        /// </summary>
        public static readonly string ussClassName = "appui-vector2field";

        /// <summary>
        /// The SeedField size styling class.
        /// </summary>
        public static readonly string sizeUssClassName = ussClassName + "--size-";

        /// <summary>
        /// The SeedField container styling class.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName + "__container";

        /// <summary>
        /// The SeedField X NumericalField styling class.
        /// </summary>
        public static readonly string xFieldUssClassName = ussClassName + "__x-field";

        Size m_Size;

        int m_Value;

        readonly IntField m_SeedField;
        readonly Toggle m_Checkbox;
        bool m_UserSpecified;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SeedField()
        {
            AddToClassList(ussClassName);

            var container = new VisualElement { name = containerUssClassName };
            container.AddToClassList(containerUssClassName);
            container.style.flexDirection = FlexDirection.Column;
            var label = new InputLabel(TextContent.customSeed);
            label.AddToClassList("larger-label");
            m_Checkbox = new Toggle { size = Size.S};
            m_Checkbox.RegisterValueChangedCallback(OnChangeMode);
            label.inputAlignment = Align.FlexEnd;
            label.labelOverflow = TextOverflow.Ellipsis;
            label.Add(m_Checkbox);
            container.Add(label);
            label.AddToClassList("bottom-gap");

            m_SeedField = new IntField { name = xFieldUssClassName };
            m_SeedField.style.flexGrow = 1;
            m_SeedField.lowValue = ushort.MinValue;
            m_SeedField.highValue = ushort.MaxValue;
            container.Add(m_SeedField);

            hierarchy.Add(container);

            size = Size.M;
            SetValueWithoutNotify(0);

            m_SeedField.RegisterCallback<ChangingEvent<int>>(OnIntFieldChanged);

            UpdateSeedVisual();
        }

        void OnChangeMode(ChangeEvent<bool> evt)
        {
            userSpecified = evt.newValue;
            UpdateSeedVisual();
        }

        void UpdateSeedVisual()
        {
            m_Checkbox.SetValueWithoutNotify(userSpecified);
            m_SeedField.style.display = userSpecified ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// The content container of the SeedField.
        /// </summary>
        public override VisualElement contentContainer => null;

        /// <summary>
        /// The size of the SeedField.
        /// </summary>
        public Size size
        {
            get => m_Size;
            set
            {
                RemoveFromClassList(sizeUssClassName + m_Size.ToString().ToLower());
                m_Size = value;
                AddToClassList(sizeUssClassName + m_Size.ToString().ToLower());
                m_SeedField.size = m_Size;
                m_Checkbox.size = m_Size;
            }
        }

        /// <summary>
        /// Set the value of the SeedField without notifying the change.
        /// </summary>
        /// <param name="newValue"> The new value of the Vector2IntField. </param>
        public void SetValueWithoutNotify(int newValue)
        {
            m_Value = newValue;
            m_SeedField.SetValueWithoutNotify(m_Value);
            if (validateValue != null) invalid = !validateValue(m_Value);
        }

        /// <summary>
        /// The value of the SeedField.
        /// </summary>
        public int value
        {
            get => m_Value;
            set
            {
                if (m_Value == value)
                    return;
                using var evt = ChangeEvent<int>.GetPooled(m_Value, value);
                evt.target = this;
                SetValueWithoutNotify(value);
                SendEvent(evt);
            }
        }

        /// <summary>
        /// The invalid state of the SeedField.
        /// </summary>
        public bool invalid
        {
            get => ClassListContains(Styles.invalidUssClassName);
            set
            {
                EnableInClassList(Styles.invalidUssClassName, value);

                m_SeedField.EnableInClassList(Styles.invalidUssClassName, value);
            }
        }

        /// <summary>
        /// The validation function to use to validate the value.
        /// </summary>
        public Func<int, bool> validateValue { get; set; }

        void OnIntFieldChanged(ChangingEvent<int> evt)
        {
            value = evt.newValue;
        }

        public bool userSpecified
        {
            get => m_UserSpecified;
            set
            {
                if (m_UserSpecified == value)
                    return;

                m_UserSpecified = value;
                UpdateSeedVisual();

                using var evt = ChangeEvent<int>.GetPooled(m_Value, m_Value);
                evt.target = this;
                SendEvent(evt);
            }
        }

        /// <summary>
        /// Factory class to instantiate a <see cref="UnityEngine.UIElements.Vector2IntField"/> using the data read from a UXML file.
        /// </summary>
        [Preserve]
        public new class UxmlFactory : UxmlFactory<SizeIntField, UxmlTraits> { }

        /// <summary>
        /// Class containing the <see cref="UxmlTraits"/> for the <see cref="SeedField"/>.
        /// </summary>
        public new class UxmlTraits : VisualElementExtendedUxmlTraits
        {
            readonly UxmlBoolAttributeDescription m_Disabled = new UxmlBoolAttributeDescription
            {
                name = "disabled",
                defaultValue = false
            };

            readonly UxmlEnumAttributeDescription<Size> m_Size = new UxmlEnumAttributeDescription<Size>
            {
                name = "size",
                defaultValue = Size.M,
            };

            /// <summary>
            /// Initializes the VisualElement from the UXML attributes.
            /// </summary>
            /// <param name="ve"> The <see cref="VisualElement"/> to initialize.</param>
            /// <param name="bag"> The <see cref="IUxmlAttributes"/> bag to use to initialize the <see cref="VisualElement"/>.</param>
            /// <param name="cc"> The <see cref="CreationContext"/> to use to initialize the <see cref="VisualElement"/>.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var element = (SizeIntField)ve;
                element.size = m_Size.GetValueFromBag(bag, cc);

                element.SetEnabled(!m_Disabled.GetValueFromBag(bag, cc));
            }
        }
    }
}