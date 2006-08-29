/* Copyright (c) 2006 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Configuration;
using System.Net;
using NUnit.Framework;
using Google.GData.Client;
using Google.GData.GoogleBase;


namespace Google.GData.GoogleBase.UnitTests
{
    [TestFixture]
    [Category("GoogleBase")]
    public class GBaseAttributeTypeTest
    {
        [Test]
        public void ForNameDoesNotCreateNewStandardTypes()
        {
            foreach (GBaseAttributeType type in GBaseAttributeType.AllStandardTypes)
            {
                Assert.IsTrue(Object.ReferenceEquals(type,
                                                     GBaseAttributeType.ForName(type.Name)));
            }
        }

        [Test]
        public void ForNameCreatesNewOtherTypes()
        {
            GBaseAttributeType other1 = GBaseAttributeType.ForName("sometype");
            Assert.AreEqual("sometype", other1.Name, "name");
            Assert.AreEqual(StandardGBaseAttributeTypeIds.otherType, other1.Id);
        }

        [Test]
        public void EqualsMethodTestOnStandardTypes()
        {
            foreach (GBaseAttributeType typeA in GBaseAttributeType.AllStandardTypes)
            {
                Assert.IsFalse(typeA.Equals(null), "null");
                Assert.IsFalse(typeA.Equals(this), "is not GBaseAttributeType");

                foreach (GBaseAttributeType typeB in GBaseAttributeType.AllStandardTypes)
                {
                    if (!Object.ReferenceEquals(typeA, typeB))
                    {
                        AssertTypesAreDifferent(typeA, typeB);
                    }
                    else
                    {
                        AssertTypesAreSame(typeA, typeB);
                    }
                }
            }
        }


        [Test]
        public void EqualsMethodTestOnOtherTypes()
        {
            AssertTypesAreSame(GBaseAttributeType.ForName("xxx"),
                               GBaseAttributeType.ForName("xxx"));
            AssertTypesAreDifferent(GBaseAttributeType.ForName("xxx"),
                                    GBaseAttributeType.ForName("xyx"));
        }

        [Test]
        public void IsSameOrSubtype()
        {
            Assert.IsTrue(GBaseAttributeType.Number
                          .IsSupertypeOf(GBaseAttributeType.Number));
            Assert.IsTrue(GBaseAttributeType.Number
                          .IsSupertypeOf(GBaseAttributeType.Int));

            Assert.IsTrue(GBaseAttributeType.Number
                          .IsSupertypeOf(GBaseAttributeType.Float));
            Assert.IsFalse(GBaseAttributeType.Float
                           .IsSupertypeOf(GBaseAttributeType.Number));
        }

        [Test]
        public void testIsSameOrSubtypeTransitivity()
        {
            Assert.IsTrue(GBaseAttributeType.DateTimeRange
                          .IsSupertypeOf(GBaseAttributeType.Date));
            Assert.IsTrue(GBaseAttributeType.DateTimeRange
                          .IsSupertypeOf(GBaseAttributeType.DateTime));
            Assert.IsTrue(GBaseAttributeType.DateTimeRange
                          .IsSupertypeOf(GBaseAttributeType.DateTimeRange));
        }

        private void AssertTypesAreDifferent(GBaseAttributeType typeA,
                                             GBaseAttributeType typeB)
        {
            Assert.IsFalse(typeA.Equals(typeB), "equals");
            Assert.IsTrue(typeA != typeB, "operator !=");
            Assert.IsFalse(typeA == typeB, "operator ==");
        }

        private void AssertTypesAreSame(GBaseAttributeType typeA,
                                        GBaseAttributeType typeB)
        {
            Assert.IsTrue(typeA.Equals(typeB), "equals");
            Assert.IsFalse(typeA != typeB, "operaton !=");
            Assert.IsTrue(typeA == typeB, "operator ==");
            Assert.AreEqual(typeA.GetHashCode(), typeB.GetHashCode(), "HashCode");
        }

    }

    [TestFixture]
    [Category("GoogleBase")]
    public class GBaseAttributeTest
    {
        [Test]
        public void ParseTest()
        {
            GBaseAttribute attribute = Parse("<bozo type='number'>blah, blah</bozo>");
            Assert.AreEqual("bozo", attribute.Name);
            Assert.AreEqual(GBaseAttributeType.Number, attribute.Type);
            Assert.AreEqual("blah, blah", attribute.Content);
        }

        [Test]
        public void ParsePublicAttributeTest()
        {
            GBaseAttribute attribute =
                Parse("<bozo type='clown'>blah, blah</bozo>");
            Assert.AreEqual(false, attribute.IsPrivate);
        }

        [Test]
        public void ParsePrivateAttributeTest()
        {
            GBaseAttribute attribute =
                Parse("<bozo access='private' type='clown'>blah, blah</bozo>");
            Assert.AreEqual(true, attribute.IsPrivate);
        }

        [Test]
        public void ParseAttributeWithSpaceTest()
        {
            GBaseAttribute attribute =
                Parse("<item_type type='text'>blah, blah</item_type>");
            Assert.AreEqual("item type", attribute.Name);
        }

        [Test]
        public void ParseAttributeWithNoType()
        {
            GBaseAttribute attribute =
                Parse("<item_type>blah, blah</item_type>");
            Assert.AreEqual("item type", attribute.Name);
            Assert.AreEqual(null, attribute.Type);
        }

        [Test]
        public void GenerateTest()
        {
            AssertRereadIsSame(new GBaseAttribute("bozo",
                                                  GBaseAttributeType.ForName("clown"),
                                                  "oops !"));
        }

        [Test]
        public void GenerateTestWithSpace()
        {
            AssertRereadIsSame(new GBaseAttribute("item type",
                                                  null,
                                                  "Product"));
        }

        [Test]
        public void GeneratePrivateTest()
        {
            GBaseAttribute attribute = new GBaseAttribute("bozo",
                                       GBaseAttributeType.Int,
                                       "oops !");
            attribute.IsPrivate = true;
            AssertRereadIsSame(attribute);
        }

        private void AssertRereadIsSame(GBaseAttribute attribute)
        {
            StringWriter sw = new StringWriter();
            XmlWriter writer = new XmlTextWriter(sw);
            attribute.Save(writer);
            writer.Close();

            GBaseAttribute parsed = Parse(sw.ToString());

            Assert.AreEqual(attribute.Name, parsed.Name, "name");
            Assert.AreEqual(attribute.Type, parsed.Type, "type");
            Assert.AreEqual(attribute.Content, parsed.Content, "content");
            Assert.AreEqual(attribute.IsPrivate, parsed.IsPrivate, "private");
        }

        private GBaseAttribute Parse(String xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(new StringReader(xml));

            return GBaseAttribute.ParseGBaseAttribute(doc.DocumentElement);
        }
    }
}
