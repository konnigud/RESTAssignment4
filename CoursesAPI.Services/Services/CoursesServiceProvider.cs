using System.Collections.Generic;
using System.Linq;
using CoursesAPI.Models;
using CoursesAPI.Services.DataAccess;
using CoursesAPI.Services.Exceptions;
using CoursesAPI.Services.Models.Entities;

namespace CoursesAPI.Services.Services
{
	public class CoursesServiceProvider
	{
		private readonly IUnitOfWork _uow;

		private readonly IRepository<CourseInstance> _courseInstances;
		private readonly IRepository<TeacherRegistration> _teacherRegistrations;
		private readonly IRepository<CourseTemplate> _courseTemplates; 
		private readonly IRepository<Person> _persons;

		public CoursesServiceProvider(IUnitOfWork uow)
		{
			_uow = uow;

			_courseInstances      = _uow.GetRepository<CourseInstance>();
			_courseTemplates      = _uow.GetRepository<CourseTemplate>();
			_teacherRegistrations = _uow.GetRepository<TeacherRegistration>();
			_persons              = _uow.GetRepository<Person>();
		}

		/// <summary>
		/// You should implement this function, such that all tests will pass.
		/// </summary>
		/// <param name="courseInstanceID">The ID of the course instance which the teacher will be registered to.</param>
		/// <param name="model">The data which indicates which person should be added as a teacher, and in what role.</param>
		/// <returns>Should return basic information about the person.</returns>
		public PersonDTO AddTeacherToCourse(int courseInstanceID, AddTeacherViewModel model)
		{
            // TODO: implement this logic!
            var course = (from c in _courseInstances.All()
                          where c.ID == courseInstanceID
                          select c).SingleOrDefault();
            
            if(course == null)
            {
                throw new AppObjectNotFoundException();
            }

            var person = (from p in _persons.All()
                          where p.SSN == model.SSN
                          select new PersonDTO
                          {
                              Name = p.Name,
                              SSN = p.SSN
                          }).SingleOrDefault();

            if(person == null)
            {
                throw new AppObjectNotFoundException();
            }

            if (model.Type == TeacherType.MainTeacher)
            {
                var mainTeacher = (from t in _teacherRegistrations.All()
                                   where t.CourseInstanceID == courseInstanceID
                                   where t.Type == TeacherType.MainTeacher
                                   select t).SingleOrDefault();

                if (mainTeacher != null)
                {
                    throw new AppValidationException("COURSE_ALREADY_HAS_A_MAIN_TEACHER");
                }
            }

            var teacherRegistration = (from t in _teacherRegistrations.All()
                                       where t.CourseInstanceID == courseInstanceID
                                       where t.SSN == model.SSN
                                       select t).SingleOrDefault();
            if(teacherRegistration != null)
            {
                throw new AppValidationException("PERSON_ALREADY_REGISTERED_TEACHER_IN_COURSE");
            }

            teacherRegistration = new TeacherRegistration()
            {
                CourseInstanceID = courseInstanceID,
                SSN = model.SSN,
                Type = TeacherType.MainTeacher
            };

            _teacherRegistrations.Add(teacherRegistration);
            _uow.Save();
            
			return person;
		}

		/// <summary>
		/// You should write tests for this function. You will also need to
		/// modify it, such that it will correctly return the name of the main
		/// teacher of each course.
		/// </summary>
		/// <param name="semester"></param>
		/// <returns></returns>
		public List<CourseInstanceDTO> GetCourseInstancesBySemester(string semester = null)
		{
			if (string.IsNullOrEmpty(semester))
			{
				semester = "20153";
			}

            var courses = (from c in _courseInstances.All().Where(p => p.SemesterID  == semester)
                           join courseTemplates in _courseTemplates.All() on c.CourseID equals courseTemplates.CourseID into courseT
                           from ct in courseT.DefaultIfEmpty()
                           join tr in _teacherRegistrations.All().Where(p => p.Type == TeacherType.MainTeacher) on c.ID equals tr.CourseInstanceID into teacherReg
                           from t in teacherReg.DefaultIfEmpty()
                           select new CourseInstanceDTO
                           {
                               Name = ct.Name,
                               TemplateID = c.CourseID,
                               CourseInstanceID = c.ID,
                               MainTeacher = t == null ? "" : t.SSN
                           }).ToList();



            List<string> teachersSSN = new List<string>();
            foreach (var c in courses)
            {
                if (!string.IsNullOrEmpty(c.MainTeacher))
                {
                    teachersSSN.Add(c.MainTeacher);
                }
            }

            if(teachersSSN.Count > 0)
            {
                var teachers = (from p in _persons.All().Where(x => teachersSSN.Contains(x.SSN))
                                 select new
                                 {
                                     SSN = p.SSN,
                                     Name = p.Name
                                 }).ToList();

                foreach(var c in courses)
                {
                    if (!string.IsNullOrEmpty(c.MainTeacher))
                    {
                        foreach(var t in teachers)
                        {
                            if(t.SSN == c.MainTeacher)
                            {
                                c.MainTeacher = t.Name;
                            }
                        }
                    }
                    
                }
            }


    //        var courses = (from c in _courseInstances.All()
    //                       join ct in _courseTemplates.All() on c.CourseID equals ct.CourseID                           
    //                       join tr in _teacherRegistrations.All() on c.ID equals tr.CourseInstanceID into teachers
    //                       from t in teachers.DefaultIfEmpty()
    //                       join person in _persons.All() on t.SSN equals person.SSN into persons
    //                       from p in persons.DefaultIfEmpty()
    //                       where c.SemesterID == semester
    //                       //where t.Type == TeacherType.MainTeacher
    //                       select new CourseInstanceDTO
    //                       {
    //                           Name = ct.Name,
    //                           TemplateID = ct.CourseID,
    //                           CourseInstanceID = c.ID,
    //                           //MainTeacher = p == null ? "" : p.Name
				//}).ToList();

			return courses;
		}
	}
}
