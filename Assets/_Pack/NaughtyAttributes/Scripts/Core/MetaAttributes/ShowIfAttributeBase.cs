using System;

namespace NaughtyAttributes
{
    public class ShowIfAttributeBase : MetaAttribute
    {
        public string[] Conditions { get; private set; }
        public EConditionOperator ConditionOperator { get; private set; }
        public bool Inverted { get; protected set; }




        public Enum EnumValue { get; private set; }

        public ShowIfAttributeBase(string condition)
        {
            ConditionOperator = EConditionOperator.And;
            Conditions = new string[1] { condition };
        }

        public ShowIfAttributeBase(EConditionOperator conditionOperator, params string[] conditions)
        {
            ConditionOperator = conditionOperator;
            Conditions = conditions;
        }

        public ShowIfAttributeBase(string enumName, Enum enumValue)
            : this(enumName)
        {
            if (enumValue == null)
            {
                throw new ArgumentNullException(nameof(enumValue), "This parameter must be an enum value.");
            }

            EnumValue = enumValue;
        }
    }
}
